using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class PropertyRepository : Repository<Property>, IPropertyRepository
{
    public PropertyRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Property?> GetByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Rooms)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetByTypeAsync(PropertyType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Type == type)
            .ToListAsync(cancellationToken);
    }

    public async Task<Property?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // NOTE: Property does not have Contracts navigation property
        // Contracts should be loaded separately via ContractRepository
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Property>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var searchLower = searchTerm.ToLower();
        return await _dbSet
            .Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Address.ToLower().Contains(searchLower) ||
                p.City.ToLower().Contains(searchLower))
            .ToListAsync(cancellationToken);
    }
}
