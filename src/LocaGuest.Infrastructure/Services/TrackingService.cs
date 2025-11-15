using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Analytics;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Services;

/// <summary>
/// Implementation of tracking service for product analytics
/// </summary>
public class TrackingService : ITrackingService
{
    private readonly LocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        LocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext,
        ILogger<TrackingService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task TrackAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.TrackingEvents.AddAsync(trackingEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Never throw - tracking should not break the application
            _logger.LogError(ex, "Failed to save tracking event: {EventType}", trackingEvent.EventType);
        }
    }

    public async Task TrackEventAsync(
        string eventType,
        string? pageName = null,
        string? url = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("TenantId is required for tracking");
            var userId = _currentUserService.UserId ?? throw new InvalidOperationException("UserId is required for tracking");
            var ipAddress = _currentUserService.IpAddress ?? "unknown";
            var userAgent = _currentUserService.UserAgent ?? "unknown";

            // Check if user has opted out of tracking (GDPR)
            var allowTracking = await CheckUserAllowsTrackingAsync(userId, cancellationToken);
            if (!allowTracking)
            {
                _logger.LogDebug("User {UserId} has opted out of tracking", userId);
                return; // Respect user's privacy choice
            }

            var trackingEvent = TrackingEvent.Create(
                tenantId: tenantId,
                userId: userId,
                eventType: eventType,
                ipAddress: ipAddress,
                userAgent: userAgent,
                pageName: pageName,
                url: url,
                metadata: metadata
            );

            await TrackAsync(trackingEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never throw - tracking should not break the application
            _logger.LogError(ex, "Failed to track event: {EventType}", eventType);
        }
    }

    private async Task<bool> CheckUserAllowsTrackingAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var userSettings = await _context.UserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);

            // Default to true if settings not found (opt-in by default)
            return userSettings?.AllowTracking ?? true;
        }
        catch
        {
            // On error, default to allow tracking
            return true;
        }
    }
}
