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

    public async Task<IEnumerable<Contract>> GetActiveContractsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.Status == ContractStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Contract>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.RenterTenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}
