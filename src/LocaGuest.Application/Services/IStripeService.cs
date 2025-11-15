namespace LocaGuest.Application.Services;

/// <summary>
/// Service interface for Stripe payment operations
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Creates a Stripe checkout session for subscription
    /// </summary>
    Task<(string SessionId, string Url)> CreateCheckoutSessionAsync(
        Guid userId, 
        Guid planId, 
        bool isAnnual, 
        string userEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Stripe billing portal session
    /// </summary>
    Task<string> CreatePortalSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles Stripe webhook events
    /// </summary>
    Task HandleWebhookEventAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
