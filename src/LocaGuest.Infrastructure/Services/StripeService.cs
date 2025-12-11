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

    public async Task<Application.Services.StripeCheckoutSession> CreateCheckoutSessionAsync(
        string userId, 
        string planId, 
        bool isAnnual,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get plan from database to retrieve Stripe Price ID
            var plan = await _unitOfWork.Plans.GetByIdAsync(Guid.Parse(planId), cancellationToken);
            
            if (plan == null)
                throw new InvalidOperationException($"Plan {planId} not found");

            var priceId = isAnnual ? plan.StripeAnnualPriceId : plan.StripeMonthlyPriceId;
            
            if (string.IsNullOrEmpty(priceId))
                throw new InvalidOperationException(
                    $"Stripe Price ID not configured for plan {plan.Code} ({(isAnnual ? "annual" : "monthly")})");

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
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = userId,
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId },
                        { "plan_id", planId }
                    },
                    TrialPeriodDays = 14,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId },
                    { "plan_id", planId }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Checkout session created: {SessionId} for user {UserId}", 
                session.Id, userId);

            return new Application.Services.StripeCheckoutSession(session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreatePortalSessionAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl,
            };

            var service = new Stripe.BillingPortal.SessionService();
            var portalSession = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return portalSession.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating portal session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, bool immediately, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionCancelOptions
            {
                InvoiceNow = immediately,
                Prorate = immediately
            };

            await service.CancelAsync(subscriptionId, options, cancellationToken: cancellationToken);
            _logger.LogInformation("Subscription {SubscriptionId} canceled", subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error canceling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<List<Application.Services.StripeInvoice>> GetInvoicesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new InvoiceService();
            var options = new InvoiceListOptions
            {
                Customer = customerId,
                Limit = 100
            };

            var invoices = await service.ListAsync(options, cancellationToken: cancellationToken);
            return invoices.Data.Select(inv => new Application.Services.StripeInvoice(
                inv.Id,
                inv.Created,
                inv.Description,
                inv.AmountPaid,
                inv.Currency,
                inv.Status,
                inv.InvoicePdf
            )).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting invoices for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<List<Application.Services.StripePaymentMethod>> GetPaymentMethodsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService();
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await service.ListAsync(options, cancellationToken: cancellationToken);
            return paymentMethods.Data.Select(pm => new Application.Services.StripePaymentMethod(
                pm.Id,
                pm.Card.Brand,
                pm.Card.Last4,
                pm.Card.ExpMonth,
                pm.Card.ExpYear
            )).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting payment methods for customer {CustomerId}", customerId);
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
