namespace LocaGuest.Domain.Repositories;

/// <summary>
/// Unit of Work interface for managing transactions following DDD principles
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IPropertyRepository Properties { get; }
    ITenantRepository Tenants { get; }
    IContractRepository Contracts { get; }
    IDocumentRepository Documents { get; }
    ISubscriptionRepository Subscriptions { get; }
    IOrganizationRepository Organizations { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
    bool HasActiveTransaction { get; }
}
