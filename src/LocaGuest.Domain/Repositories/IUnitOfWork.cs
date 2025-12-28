namespace LocaGuest.Domain.Repositories;

/// <summary>
/// Unit of Work interface for managing transactions following DDD principles
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IPropertyRepository Properties { get; }
    IPropertyImageRepository PropertyImages { get; }
    ITenantRepository Tenants { get; }
    ITeamMemberRepository TeamMembers { get; }
    IInvitationTokenRepository InvitationTokens { get; }
    IContractRepository Contracts { get; }
    IAddendumRepository Addendums { get; }
    IInventoryEntryRepository InventoryEntries { get; }
    IInventoryExitRepository InventoryExits { get; }
    IDocumentRepository Documents { get; }
    IPlanRepository Plans { get; }
    ISubscriptionRepository Subscriptions { get; }
    IOrganizationRepository Organizations { get; }
    IPaymentRepository Payments { get; }
    IRentInvoiceRepository RentInvoices { get; }
    IUserProfileRepository UserProfiles { get; }
    IUserPreferencesRepository UserPreferences { get; }
    INotificationSettingsRepository NotificationSettings { get; }
    IUserSessionRepository UserSessions { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
    bool HasActiveTransaction { get; }
}
