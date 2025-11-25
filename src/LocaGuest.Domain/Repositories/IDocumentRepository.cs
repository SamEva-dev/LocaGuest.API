using LocaGuest.Domain.Aggregates.DocumentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
}
