using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Jobs;

/// <summary>
/// Background job to clean up old tracking events (GDPR retention policy)
/// Run daily to delete events older than retention period
/// </summary>
public class TrackingRetentionJob
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<TrackingRetentionJob> _logger;
    
    // Configurable retention periods
    private readonly int _retentionDays;

    public TrackingRetentionJob(
        LocaGuestDbContext context,
        ILogger<TrackingRetentionJob> logger,
        int retentionDays = 90) // Default: 90 days
    {
        _context = context;
        _logger = logger;
        _retentionDays = retentionDays;
    }

    /// <summary>
    /// Execute the retention job - delete old tracking events
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting tracking retention job (retention: {RetentionDays} days)", _retentionDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            // Delete old tracking events
            var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM tracking_events 
                  WHERE ""Timestamp"" < {0}",
                cutoffDate,
                cancellationToken);

            _logger.LogInformation(
                "Tracking retention job completed. Deleted {DeletedCount} events older than {CutoffDate}",
                deletedCount,
                cutoffDate);

            // Optionally vacuum the table to reclaim space (PostgreSQL)
            await _context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE tracking_events", cancellationToken);

            _logger.LogInformation("Database vacuum completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tracking retention job");
            throw;
        }
    }

    /// <summary>
    /// Archive old events to cold storage before deleting (optional)
    /// </summary>
    public async Task ArchiveAndDeleteAsync(string archiveConnectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting tracking archive and retention job");

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            // 1. First, copy old events to archive database (if configured)
            if (!string.IsNullOrEmpty(archiveConnectionString))
            {
                // This would require a separate DbContext for archive DB
                _logger.LogInformation("Archiving events older than {CutoffDate} to cold storage", cutoffDate);
                
                // TODO: Implement archive logic
                // - Copy to archive database
                // - Or export to file (Parquet, CSV)
                // - Or send to data warehouse
            }

            // 2. Then delete from main database
            await ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tracking archive and retention job");
            throw;
        }
    }

    /// <summary>
    /// Anonymize user data for GDPR "right to be forgotten"
    /// </summary>
    public async Task AnonymizeUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Anonymizing tracking data for user {UserId}", userId);

            var anonymizedCount = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE tracking_events 
                  SET 
                    ""UserId"" = '00000000-0000-0000-0000-000000000000',
                    ""IpAddress"" = '0.0.0.0',
                    ""UserAgent"" = 'anonymized',
                    ""Metadata"" = NULL
                  WHERE ""UserId"" = {0}",
                userId,
                cancellationToken);

            _logger.LogInformation(
                "Anonymized {AnonymizedCount} tracking events for user {UserId}",
                anonymizedCount,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anonymizing tracking data for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get retention statistics
    /// </summary>
    public async Task<RetentionStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        var totalEvents = await _context.TrackingEvents.CountAsync(cancellationToken);
        var oldEvents = await _context.TrackingEvents.CountAsync(e => e.Timestamp < cutoffDate, cancellationToken);
        var oldestEvent = await _context.TrackingEvents
            .OrderBy(e => e.Timestamp)
            .Select(e => e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return new RetentionStats
        {
            TotalEvents = totalEvents,
            EventsToDelete = oldEvents,
            OldestEventDate = oldestEvent,
            RetentionDays = _retentionDays,
            CutoffDate = cutoffDate
        };
    }
}

/// <summary>
/// Retention statistics
/// </summary>
public class RetentionStats
{
    public int TotalEvents { get; set; }
    public int EventsToDelete { get; set; }
    public DateTime? OldestEventDate { get; set; }
    public int RetentionDays { get; set; }
    public DateTime CutoffDate { get; set; }
}
