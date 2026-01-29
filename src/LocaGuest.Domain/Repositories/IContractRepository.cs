using LocaGuest.Domain.Aggregates.ContractAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IContractRepository : IRepository<Contract>
{
    Task<IEnumerable<Contract>> GetActiveContractsAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Contract>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Contract>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<Contract>> GetContractsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Contract>> GetByTypeAsync(ContractType type, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<Contract?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false);
}
