using LocaGuest.Domain.Audit;

namespace LocaGuest.Application.Services;

/// <summary>
/// Service for logging audit entries
/// </summary>
public interface IAuditService
{
    Task LogCommandAsync(CommandAuditLog auditLog, CancellationToken cancellationToken = default);
    Task LogEntityChangeAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
