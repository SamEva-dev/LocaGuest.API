using LocaGuest.Domain.Aggregates.DepositAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class DepositRepository : Repository<Deposit>, IDepositRepository
{
    public DepositRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Deposit?> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Deposit> query = _context.Set<Deposit>().Include(d => d.Transactions);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(d => d.ContractId == contractId, cancellationToken);
    }
}
