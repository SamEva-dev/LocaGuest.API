namespace LocaGuest.Application.Services;

public record StripeCheckoutSession(string Id, string Url);
public record StripeCheckoutSessionSummary(string? Status, string? CustomerEmail, string? SubscriptionId);
public record StripeInvoice(string Id, DateTime Created, string? Description, long AmountPaid, string Currency, string Status, string? InvoicePdf);
public record StripePaymentMethod(string Id, string Brand, string Last4, long ExpMonth, long ExpYear);

/// <summary>
/// Service interface for Stripe payment operations
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Creates a Stripe checkout session for subscription
    /// </summary>
    Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        string userId, 
        string planId, 
        bool isAnnual,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Stripe billing portal session
    /// </summary>
    Task<string> CreatePortalSessionAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription
    /// </summary>
    Task CancelSubscriptionAsync(string subscriptionId, bool immediately, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices for a customer
    /// </summary>
    Task<List<StripeInvoice>> GetInvoicesAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment methods for a customer
    /// </summary>
    Task<List<StripePaymentMethod>> GetPaymentMethodsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles Stripe webhook events
    /// </summary>
    Task HandleWebhookEventAsync(string payload, string signature, CancellationToken cancellationToken = default);

    Task<StripeCheckoutSessionSummary> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
