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

    public async Task<IEnumerable<Document>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Document> query = _dbSet
            .Where(d => d.AssociatedOccupantId == tenantId && !d.IsArchived)
            .OrderByDescending(d => d.CreatedAt);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Document> query = _dbSet
            .Where(d => d.PropertyId == propertyId && !d.IsArchived)
            .OrderByDescending(d => d.CreatedAt);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<ContractDocumentLink> links = _context.Set<ContractDocumentLink>().AsQueryable();
        IQueryable<Document> docs = _dbSet.AsQueryable();

        if (asNoTracking)
        {
            links = links.AsNoTracking();
            docs = docs.AsNoTracking();
        }

        return await links
            .Where(link => link.ContractId == contractId)
            .Join(
                docs,
                link => link.DocumentId,
                doc => doc.Id,
                (_, doc) => doc)
            .Where(doc => !doc.IsArchived)
            .OrderByDescending(doc => doc.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
