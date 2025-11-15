using LocaGuest.Domain.Aggregates.TenantAggregate;

namespace LocaGuest.Domain.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
}
