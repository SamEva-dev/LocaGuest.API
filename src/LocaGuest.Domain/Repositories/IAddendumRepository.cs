using LocaGuest.Domain.Aggregates.ContractAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IAddendumRepository : IRepository<Addendum>
{
    Task<IEnumerable<Addendum>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
}
