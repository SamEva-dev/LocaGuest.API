using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active", cancellationToken);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Status == "active")
            .ToListAsync(cancellationToken);
    }
}
