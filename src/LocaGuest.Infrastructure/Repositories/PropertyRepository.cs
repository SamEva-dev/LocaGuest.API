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

    public async Task<Property?> GetByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Property> query = _dbSet.Include(p => p.Rooms);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetByStatusAsync(PropertyStatus status, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Property> query = _dbSet.Where(p => p.Status == status);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Property>> GetByTypeAsync(PropertyType type, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Property> query = _dbSet.Where(p => p.Type == type);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Property?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        // NOTE: Property does not have Contracts navigation property
        // Contracts should be loaded separately via ContractRepository
        IQueryable<Property> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Property>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var searchLower = searchTerm.ToLower();
        IQueryable<Property> query = _dbSet
            .Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Address.ToLower().Contains(searchLower) ||
                p.City.ToLower().Contains(searchLower))
            .AsQueryable();

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }
}
