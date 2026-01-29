using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class OccupantRepository : Repository<Occupant>, IOccupantRepository
{
    public OccupantRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Occupant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Occupant> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(t => t.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Occupant>> GetActiveOccupantsAsync(CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Occupant> query = _dbSet.Where(t => t.Status == OccupantStatus.Active);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }
}
