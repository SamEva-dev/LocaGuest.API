using LocaGuest.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Common.Interfaces;

public interface IAuditDbContext
{
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<CommandAuditLog> CommandAuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
