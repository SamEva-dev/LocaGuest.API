using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class ContractRepository : Repository<Contract>, IContractRepository
{
    public ContractRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Contract>> GetActiveContractsAsync(CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Where(c => c.Status == ContractStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Where(c => c.PropertyId == propertyId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Where(c => c.RenterOccupantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<List<Contract>> GetContractsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query
            .Where(c => c.RenterOccupantId == tenantId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Where(c => c.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByTypeAsync(ContractType type, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Where(c => c.Type == type).ToListAsync(cancellationToken);
    }

    public async Task<Contract?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Contract> query = _dbSet;
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
