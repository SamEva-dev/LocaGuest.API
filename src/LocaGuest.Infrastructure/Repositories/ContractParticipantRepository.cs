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
        try
        {
            return await _dbSet
                        .AsNoTracking()
                        .Where(p => p.ContractId == contractId
                                    && p.StartDate <= d
                                    && (!p.EndDate.HasValue || p.EndDate.Value >= d))
                        .OrderBy(p => p.StartDate)
                        .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        
    }

    public async Task<List<ContractParticipant>> GetOverlappingByContractIdAsync(Guid contractId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default)
    {
        var start = periodStartUtc.Kind == DateTimeKind.Utc
            ? periodStartUtc
            : periodStartUtc.Kind == DateTimeKind.Local ? periodStartUtc.ToUniversalTime() : DateTime.SpecifyKind(periodStartUtc, DateTimeKind.Utc);

        var end = periodEndUtc.Kind == DateTimeKind.Utc
            ? periodEndUtc
            : periodEndUtc.Kind == DateTimeKind.Local ? periodEndUtc.ToUniversalTime() : DateTime.SpecifyKind(periodEndUtc, DateTimeKind.Utc);

        if (end < start)
            throw new ArgumentException("periodEndUtc must be greater than or equal to periodStartUtc");

        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ContractId == contractId
                        && p.StartDate <= end
                        && (!p.EndDate.HasValue || p.EndDate.Value >= start))
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }
}
