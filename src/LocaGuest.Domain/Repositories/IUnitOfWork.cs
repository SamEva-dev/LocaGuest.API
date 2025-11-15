namespace LocaGuest.Domain.Repositories;

/// <summary>
/// Unit of Work interface for managing transactions following DDD principles
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IPropertyRepository Properties { get; }
    IContractRepository Contracts { get; }
    ITenantRepository Tenants { get; }
    ISubscriptionRepository Subscriptions { get; }

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
    bool HasActiveTransaction { get; }
}
