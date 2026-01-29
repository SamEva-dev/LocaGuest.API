using LocaGuest.Domain.Aggregates.PropertyAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Property>> GetByTypeAsync(PropertyType type, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<Property?> GetByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<Property?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Property>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default, bool asNoTracking = false);
}
