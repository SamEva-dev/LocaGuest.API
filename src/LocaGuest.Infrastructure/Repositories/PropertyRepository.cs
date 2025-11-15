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

    public async Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Property?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // NOTE: Property does not have Contracts navigation property
        // Contracts should be loaded separately via ContractRepository
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
