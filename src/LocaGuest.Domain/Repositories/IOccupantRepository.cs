using LocaGuest.Domain.Aggregates.OccupantAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IOccupantRepository : IRepository<Occupant>
{
    Task<Occupant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Occupant>> GetActiveOccupantsAsync(CancellationToken cancellationToken = default);
}
