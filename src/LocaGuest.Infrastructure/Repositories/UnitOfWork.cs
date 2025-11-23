using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;

namespace LocaGuest.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation managing transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly LocaGuestDbContext _context;
    private IPropertyRepository? _properties;
    private IContractRepository? _contracts;
    private ITenantRepository? _tenants;
    private IDocumentRepository? _documents;
    private ISubscriptionRepository? _subscriptions;
    private IOrganizationRepository? _organizations;

    public UnitOfWork(LocaGuestDbContext context)
    {
        _context = context;
    }

    public IPropertyRepository Properties => 
        _properties ??= new PropertyRepository(_context);

    public IContractRepository Contracts => 
        _contracts ??= new ContractRepository(_context);

    public ITenantRepository Tenants => 
        _tenants ??= new TenantRepository(_context);

    public IDocumentRepository Documents => 
        _documents ??= new DocumentRepository(_context);

    public ISubscriptionRepository Subscriptions => 
        _subscriptions ??= new SubscriptionRepository(_context);

    public IOrganizationRepository Organizations => 
        _organizations ??= new OrganizationRepository(_context);

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public Task RollbackAsync()
    {
        // EF Core doesn't require explicit rollback as changes aren't saved
        // Clear change tracker to discard all changes
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
        return Task.CompletedTask;
    }

    public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

    public void Dispose()
    {
        _context.Dispose();
    }
}
