using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeEvent = Stripe.Event;
using StripeSession = Stripe.Checkout.Session;

namespace LocaGuest.Infrastructure.Services;

/// <summary>
/// Stripe service implementation handling payment operations
/// </summary>
public class StripeService : IStripeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<StripeService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    public async Task<(string SessionId, string Url)> CreateCheckoutSessionAsync(
        Guid userId, 
        Guid planId, 
        bool isAnnual, 
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await _unitOfWork.Properties.GetByIdAsync(planId, cancellationToken);
            if (plan == null)
                throw new ArgumentException($"Plan {planId} not found");

            // NOTE: Temporaire - utiliser _unitOfWork.Plans quand le repository sera créé
            var priceId = isAnnual ? "stripe_annual_price_id" : "stripe_monthly_price_id";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = _configuration["Stripe:SuccessUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _configuration["Stripe:CancelUrl"],
                CustomerEmail = userEmail,
                ClientReferenceId = userId.ToString(),
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "plan_id", planId.ToString() }
                    },
                    TrialPeriodDays = 14,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "plan_id", planId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Checkout session created: {SessionId} for user {UserId}", 
                session.Id, userId);

            return (session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, cancellationToken);
            
            if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
                throw new InvalidOperationException("No active subscription found");

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = subscription.StripeCustomerId,
                ReturnUrl = _configuration["Stripe:CancelUrl"],
            };

            var service = new Stripe.BillingPortal.SessionService();
            var portalSession = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return portalSession.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating portal session for user {UserId}", userId);
            throw;
        }
    }

    public async Task HandleWebhookEventAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
            
            _logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook processing error");
            throw;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(StripeEvent stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as StripeSession;
        if (session == null) return;

        if (!session.Metadata.TryGetValue("user_id", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("User ID not found in session metadata");
            return;
        }

        if (!session.Metadata.TryGetValue("plan_id", out var planIdStr) ||
            !Guid.TryParse(planIdStr, out var planId))
        {
            _logger.LogWarning("Plan ID not found in session metadata");
            return;
        }

        var subscription = Domain.Aggregates.SubscriptionAggregate.Subscription.Create(
            userId, planId, isAnnual: false, trialDays: 14);

        subscription.SetStripeInfo(session.CustomerId ?? "", session.SubscriptionId ?? "");

        await _unitOfWork.Subscriptions.AddAsync(subscription, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Subscription created for user {UserId}", userId);
    }

    private async Task HandleSubscriptionUpdatedAsync(StripeEvent stripeEvent, CancellationToken cancellationToken)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _unitOfWork.Subscriptions
            .GetByStripeSubscriptionIdAsync(stripeSubscription.Id, cancellationToken);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found: {SubscriptionId}", stripeSubscription.Id);
            return;
        }

        subscription.UpdateStatus(stripeSubscription.Status);
        _unitOfWork.Subscriptions.Update(subscription);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Subscription {SubscriptionId} updated to {Status}", 
            subscription.Id, stripeSubscription.Status);
    }

    private async Task HandleSubscriptionDeletedAsync(StripeEvent stripeEvent, CancellationToken cancellationToken)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _unitOfWork.Subscriptions
            .GetByStripeSubscriptionIdAsync(stripeSubscription.Id, cancellationToken);

        if (subscription != null)
        {
            subscription.Cancel();
            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.CommitAsync(cancellationToken);
            
            _logger.LogInformation("Subscription {SubscriptionId} canceled", subscription.Id);
        }
    }

    private async Task HandleInvoicePaymentSucceededAsync(StripeEvent stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice paid: {InvoiceId} for {Amount}",
            invoice.Id, invoice.AmountPaid / 100.0);

        // Logic already handled by subscription.updated event
        await Task.CompletedTask;
    }

    private async Task HandleInvoicePaymentFailedAsync(StripeEvent stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}", invoice.Id);

        // Logic already handled by subscription.updated event
        await Task.CompletedTask;
    }
}
