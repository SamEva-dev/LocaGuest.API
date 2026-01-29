using LocaGuest.Domain.Aggregates.OccupantAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IOccupantRepository : IRepository<Occupant>
{
    Task<Occupant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<Occupant>> GetActiveOccupantsAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
}
