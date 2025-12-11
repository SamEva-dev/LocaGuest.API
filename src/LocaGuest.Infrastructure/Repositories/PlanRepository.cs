using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class PlanRepository : Repository<Plan>, IPlanRepository
{
    public PlanRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<List<Plan>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
