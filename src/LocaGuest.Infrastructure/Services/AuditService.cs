using LocaGuest.Application.Services;
using LocaGuest.Domain.Audit;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Services;

/// <summary>
/// Implementation of audit service using dedicated Audit database
/// </summary>
public class AuditService : IAuditService
{
    private readonly AuditDbContext _auditDbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AuditDbContext auditDbContext,
        ILogger<AuditService> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task LogCommandAsync(CommandAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditDbContext.CommandAuditLogs.AddAsync(auditLog, cancellationToken);
            await _auditDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if audit logging fails
            _logger.LogError(ex, "Failed to save command audit log for {CommandName}", auditLog.CommandName);
        }
    }

    public async Task LogEntityChangeAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditDbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _auditDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if audit logging fails
            _logger.LogError(ex, "Failed to save entity audit log for {EntityType} {EntityId}", 
                auditLog.EntityType, auditLog.EntityId);
        }
    }
}
