using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Document>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.AssociatedTenantId == tenantId && !d.IsArchived)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.PropertyId == propertyId && !d.IsArchived)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ContractDocumentLink>()
            .AsNoTracking()
            .Where(link => link.ContractId == contractId)
            .Join(
                _dbSet.AsNoTracking(),
                link => link.DocumentId,
                doc => doc.Id,
                (_, doc) => doc)
            .Where(doc => !doc.IsArchived)
            .OrderByDescending(doc => doc.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
