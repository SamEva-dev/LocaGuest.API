using LocaGuest.Domain.Aggregates.DocumentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<List<Document>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
}
