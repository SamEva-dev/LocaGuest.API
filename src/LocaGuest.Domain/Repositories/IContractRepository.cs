using LocaGuest.Domain.Aggregates.ContractAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IContractRepository : IRepository<Contract>
{
    Task<IEnumerable<Contract>> GetActiveContractsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Contract>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Contract>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<Contract>> GetContractsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Contract>> GetByTypeAsync(ContractType type, CancellationToken cancellationToken = default);
    Task<Contract?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
