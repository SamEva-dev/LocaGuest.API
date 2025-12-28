using LocaGuest.Domain.Aggregates.ContractAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IContractParticipantRepository : IRepository<ContractParticipant>
{
    Task<List<ContractParticipant>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<List<ContractParticipant>> GetEffectiveByContractIdAtDateAsync(Guid contractId, DateTime dateUtc, CancellationToken cancellationToken = default);
}
