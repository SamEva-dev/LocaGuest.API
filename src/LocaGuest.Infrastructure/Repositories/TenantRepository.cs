using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class TenantRepository : Repository<Occupant>, IOccupantRepository
{
    public TenantRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Occupant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Occupant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Status == OccupantStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
