using LocaGuest.Domain.Aggregates.SubscriptionAggregate;

namespace LocaGuest.Domain.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
}
