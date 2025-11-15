using LocaGuest.Domain.Analytics;

namespace LocaGuest.Application.Services;

/// <summary>
/// Service for tracking user behavior and product analytics
/// Separate from IAuditService (security audit)
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Track an event with automatic context injection
    /// </summary>
    Task TrackAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Track an event with minimal parameters (convenience method)
    /// </summary>
    Task TrackEventAsync(
        string eventType,
        string? pageName = null,
        string? url = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
