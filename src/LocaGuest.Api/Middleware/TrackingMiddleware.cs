using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Analytics;
using System.Diagnostics;

namespace LocaGuest.Api.Middleware;

/// <summary>
/// Middleware to automatically track all authenticated API requests
/// </summary>
public class TrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TrackingMiddleware> _logger;

    // Excluded paths that should not be tracked
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/live",
        "/ready",
        "/metrics",
        "/swagger",
        "/tracking/event",
        "/_framework",
        "/.well-known"
    };

    public TrackingMiddleware(RequestDelegate next, ILogger<TrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IServiceScopeFactory serviceScopeFactory,
        ICurrentUserService currentUserService,
        IOrganizationContext orgContext)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip tracking for excluded paths
        if (ShouldSkipTracking(path))
        {
            await _next(context);
            return;
        }

        // Continue processing the request
        await _next(context);
        
        stopwatch.Stop();

        // Track only authenticated requests
        if (currentUserService.IsAuthenticated)
        {
            try
            {
                var organizationId = orgContext.OrganizationId;
                var userId = currentUserService.UserId;

                if (organizationId.HasValue && userId.HasValue)
                {
                    var trackingEvent = TrackingEvent.Create(
                        organizationId: organizationId.Value,
                        userId: userId.Value,
                        eventType: TrackingEventTypes.ApiRequest,
                        ipAddress: currentUserService.IpAddress ?? "unknown",
                        userAgent: currentUserService.UserAgent ?? "unknown",
                        url: $"{context.Request.Method} {path}",
                        metadata: null // Don't include body for security/GDPR
                    );

                    trackingEvent.SetPerformanceMetrics(
                        durationMs: (int)stopwatch.ElapsedMilliseconds,
                        statusCode: context.Response.StatusCode
                    );

                    // Fire and forget with a new scope to avoid ObjectDisposedException
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = serviceScopeFactory.CreateScope();
                            var trackingServiceScoped = scope.ServiceProvider.GetRequiredService<ITrackingService>();
                            await trackingServiceScoped.TrackAsync(trackingEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background tracking failed for: {Path}", path);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Never throw - tracking should not break the application
                _logger.LogError(ex, "Failed to track API request: {Path}", path);
            }
        }
    }

    private static bool ShouldSkipTracking(string path)
    {
        return ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for TrackingMiddleware
/// </summary>
public static class TrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TrackingMiddleware>();
    }
}
