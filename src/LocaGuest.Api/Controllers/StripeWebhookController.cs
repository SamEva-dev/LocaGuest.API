using Microsoft.AspNetCore.Mvc;
using Stripe;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly IConfiguration _configuration;

    public StripeWebhookController(
        ILocaGuestDbContext context,
        ILogger<StripeWebhookController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                webhookSecret
            );

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            return BadRequest();
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        _logger.LogInformation("Checkout completed for customer: {CustomerId}", session.CustomerId);
        
        // Récupérer les métadonnées
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

        // Créer l'abonnement dans la base
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null) return;

        var subscription = Domain.Aggregates.SubscriptionAggregate.Subscription.Create(
            userId,
            planId,
            isAnnual: false,
            trialDays: 14
        );

        subscription.SetStripeInfo(session.CustomerId ?? "", session.SubscriptionId ?? "");

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription created for user {UserId}", userId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        _logger.LogInformation("Subscription updated: {SubscriptionId}, Status: {Status}",
            stripeSubscription.Id, stripeSubscription.Status);

        // Trouver l'abonnement dans la base
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found in database: {SubscriptionId}", stripeSubscription.Id);
            return;
        }

        // Mettre à jour le statut
        subscription.UpdateStatus(stripeSubscription.Status);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Subscription {SubscriptionId} updated to status {Status}", 
            subscription.Id, stripeSubscription.Status);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        _logger.LogInformation("Subscription deleted: {SubscriptionId}", stripeSubscription.Id);

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription != null)
        {
            subscription.Cancel();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Subscription {SubscriptionId} marked as canceled", subscription.Id);
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice paid: {InvoiceId} for {Amount}",
            invoice.Id, invoice.AmountPaid / 100.0);

        // NOTE: Dans Stripe.net v49+, l'accès au subscription depuis Invoice nécessite
        // une expansion. Pour l'instant, on gère via les events subscription.updated
        // TODO: Implémenter avec invoice.Lines.Data[0].Subscription ou utiliser les expand options
        await Task.CompletedTask;
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}", invoice.Id);

        // NOTE: Dans Stripe.net v49+, l'accès au subscription depuis Invoice nécessite
        // une expansion. Pour l'instant, on gère via les events subscription.updated
        // TODO: Implémenter avec invoice.Lines.Data[0].Subscription ou utiliser les expand options
        await Task.CompletedTask;
    }
}
