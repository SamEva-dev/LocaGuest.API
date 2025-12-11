using LocaGuest.Domain.Aggregates.SubscriptionAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IPlanRepository : IRepository<Plan>
{
    Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Plan>> GetActiveAsync(CancellationToken cancellationToken = default);
}
