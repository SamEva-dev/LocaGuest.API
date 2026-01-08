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

    public async Task<Deposit?> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Deposit>()
            .Include(d => d.Transactions)
            .FirstOrDefaultAsync(d => d.ContractId == contractId, cancellationToken);
    }
}
