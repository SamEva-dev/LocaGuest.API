using LocaGuest.Domain.Aggregates.TenantAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IOccupantRepository : IRepository<Occupant>
{
    Task<Occupant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Occupant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
}
