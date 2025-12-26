using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
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
    private IPropertyImageRepository? _propertyImages;
    private IContractRepository? _contracts;
    private ITenantRepository? _tenants;
    private ITeamMemberRepository? _teamMembers;
    private IInvitationTokenRepository? _invitationTokens;
    private IDocumentRepository? _documents;
    private IPlanRepository? _plans;
    private ISubscriptionRepository? _subscriptions;
    private IOrganizationRepository? _organizations;
    private IPaymentRepository? _payments;
    private IRentInvoiceRepository? _rentInvoices;
    private IUserProfileRepository? _userProfiles;
    private IUserPreferencesRepository? _userPreferences;
    private INotificationSettingsRepository? _notificationSettings;
    private IUserSessionRepository? _userSessions;
    private IInventoryEntryRepository? _inventoryEntries;
    private IInventoryExitRepository? _inventoryExits;

    public UnitOfWork(LocaGuestDbContext context)
    {
        _context = context;
    }

    public IPropertyRepository Properties => 
        _properties ??= new PropertyRepository(_context);

    public IPropertyImageRepository PropertyImages => 
        _propertyImages ??= new PropertyImageRepository(_context);

    public IContractRepository Contracts => 
        _contracts ??= new ContractRepository(_context);

    public IInventoryEntryRepository InventoryEntries =>
        _inventoryEntries ??= new InventoryEntryRepository(_context);

    public IInventoryExitRepository InventoryExits =>
        _inventoryExits ??= new InventoryExitRepository(_context);

    public ITenantRepository Tenants => 
        _tenants ??= new TenantRepository(_context);

    public ITeamMemberRepository TeamMembers => 
        _teamMembers ??= new TeamMemberRepository(_context);

    public IInvitationTokenRepository InvitationTokens => 
        _invitationTokens ??= new InvitationTokenRepository(_context);

    public IDocumentRepository Documents => 
        _documents ??= new DocumentRepository(_context);

    public IPlanRepository Plans => 
        _plans ??= new PlanRepository(_context);

    public ISubscriptionRepository Subscriptions => 
        _subscriptions ??= new SubscriptionRepository(_context);

    public IOrganizationRepository Organizations => 
        _organizations ??= new OrganizationRepository(_context);

    public IPaymentRepository Payments => 
        _payments ??= new PaymentRepository(_context);

    public IRentInvoiceRepository RentInvoices => 
        _rentInvoices ??= new RentInvoiceRepository(_context);

    public IUserProfileRepository UserProfiles => 
        _userProfiles ??= new UserProfileRepository(_context);

    public IUserPreferencesRepository UserPreferences => 
        _userPreferences ??= new UserPreferencesRepository(_context);

    public INotificationSettingsRepository NotificationSettings => 
        _notificationSettings ??= new NotificationSettingsRepository(_context);

    public IUserSessionRepository UserSessions => 
        _userSessions ??= new UserSessionRepository(_context);

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
