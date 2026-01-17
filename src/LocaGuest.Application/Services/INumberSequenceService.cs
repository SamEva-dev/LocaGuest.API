namespace LocaGuest.Application.Services;

/// <summary>
/// Service for generating unique sequential codes for entities within a tenant
/// Format: {TenantCode}-{Prefix}{Number:0000}
/// Examples: T0001-APP0001, T0003-L0042, T0005-CTR0123
/// </summary>
public interface INumberSequenceService
{
    /// <summary>
    /// Generates the next sequential code for an entity type within a tenant
    /// </summary>
    /// <param name="OccupantId">Organization (Tenant) ID</param>
    /// <param name="entityPrefix">Entity prefix (APP, L, M, CTR, PAY, INV...)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated code (e.g., "T0003-APP0001")</returns>
    Task<string> GenerateNextCodeAsync(Guid organizationId, string entityPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last generated number for an entity type within a tenant
    /// </summary>
    /// <param name="OccupantId">Organization (Tenant) ID</param>
    /// <param name="entityPrefix">Entity prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last number or 0 if no sequences exist</returns>
    Task<int> GetLastNumberAsync(Guid organizationId, string entityPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tenant number for organisation (tenant) specific sequences
    /// </summary>
    /// <param name="OccupantId">Organization (Tenant) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last number or 0 if no sequences exist</returns>
    Task<string> GetTenantNumberAsync(Guid organizationId,  CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a sequence to a specific number (admin/maintenance only)
    /// </summary>
    /// <param name="OccupantId">Organization (Tenant) ID</param>
    /// <param name="entityPrefix">Entity prefix</param>
    /// <param name="newNumber">New starting number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetSequenceAsync(Guid organizationId, string entityPrefix, int newNumber, CancellationToken cancellationToken = default);
}
