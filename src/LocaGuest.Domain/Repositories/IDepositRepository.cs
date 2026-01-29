using LocaGuest.Domain.Aggregates.DepositAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IDepositRepository : IRepository<Deposit>
{
    Task<Deposit?> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default, bool asNoTracking = false);
}
