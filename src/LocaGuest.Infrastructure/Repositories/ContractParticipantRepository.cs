using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class ContractParticipantRepository : Repository<ContractParticipant>, IContractParticipantRepository
{
    public ContractParticipantRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<List<ContractParticipant>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ContractId == contractId)
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ContractParticipant>> GetEffectiveByContractIdAtDateAsync(Guid contractId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var d = dateUtc.Kind == DateTimeKind.Utc
            ? dateUtc
            : dateUtc.Kind == DateTimeKind.Local ? dateUtc.ToUniversalTime() : DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);

        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ContractId == contractId
                        && p.StartDate <= d
                        && (!p.EndDate.HasValue || p.EndDate.Value >= d))
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }
}
