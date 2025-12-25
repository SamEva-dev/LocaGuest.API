using LocaGuest.Domain.Aggregates.PropertyAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Property>> GetByTypeAsync(PropertyType type, CancellationToken cancellationToken = default);
    Task<Property?> GetByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Property?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Property>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
