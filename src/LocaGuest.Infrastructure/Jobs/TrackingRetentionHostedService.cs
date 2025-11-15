using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Jobs;

/// <summary>
/// Background hosted service that runs the tracking retention job daily
/// Note: To enable, uncomment the registration in Program.cs
/// </summary>
public class TrackingRetentionHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrackingRetentionHostedService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24); // Run daily
    private readonly int _retentionDays;

    public TrackingRetentionHostedService(
        IServiceProvider serviceProvider,
        ILogger<TrackingRetentionHostedService> logger,
        int retentionDays = 90)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _retentionDays = retentionDays;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tracking Retention Hosted Service started");

        // Wait for initial delay (run at 2 AM UTC)
        await WaitForScheduledTime(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Executing tracking retention job");

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Persistence.LocaGuestDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<TrackingRetentionJob>>();

                var job = new TrackingRetentionJob(context, logger, _retentionDays);
                
                // Get stats before deletion
                var statsBefore = await job.GetStatsAsync(stoppingToken);
                _logger.LogInformation(
                    "Retention job stats: Total events: {Total}, Events to delete: {ToDelete}, Oldest: {Oldest}",
                    statsBefore.TotalEvents,
                    statsBefore.EventsToDelete,
                    statsBefore.OldestEventDate);

                // Execute retention job
                await job.ExecuteAsync(stoppingToken);

                _logger.LogInformation("Tracking retention job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tracking retention job");
            }

            // Wait for next run (24 hours)
            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Tracking Retention Hosted Service stopped");
    }

    private async Task WaitForScheduledTime(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc); // 2 AM UTC

        if (now > scheduledTime)
        {
            // If past 2 AM today, schedule for 2 AM tomorrow
            scheduledTime = scheduledTime.AddDays(1);
        }

        var delay = scheduledTime - now;
        _logger.LogInformation("Next tracking retention job scheduled at {ScheduledTime} (in {Delay})", scheduledTime, delay);

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }
}
