using LocaGuest.Domain.Aggregates.SubscriptionAggregate;

namespace LocaGuest.Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Plan> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFeatureAsync(Guid userId, string featureName, CancellationToken cancellationToken = default);
    Task<bool> CheckQuotaAsync(Guid userId, string dimension, CancellationToken cancellationToken = default);
    Task<int> GetUsageAsync(Guid userId, string dimension, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(Guid userId, string dimension, int value = 1, string? metadata = null, CancellationToken cancellationToken = default);
}
