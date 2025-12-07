using LocaGuest.Domain.Aggregates.OrganizationAggregate;

namespace LocaGuest.Domain.Repositories;

/// <summary>
/// Repository interface for Organization aggregate
/// </summary>
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    IQueryable<Organization> Query();
    void Add(Organization organization);
    void Update(Organization organization);
    void Delete(Organization organization);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<int> GetLastNumberAsync(CancellationToken cancellationToken = default);
    Task<string> GetTenantNumberAsync(Guid id, CancellationToken cancellationToken = default);
}
