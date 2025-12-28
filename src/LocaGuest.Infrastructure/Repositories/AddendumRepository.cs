using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class AddendumRepository : Repository<Addendum>, IAddendumRepository
{
    public AddendumRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Addendum>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.ContractId == contractId)
            .OrderByDescending(a => a.EffectiveDate)
            .ToListAsync(cancellationToken);
    }
}
