using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

/// <summary>
/// Repository for Organization aggregate
/// </summary>
public class OrganizationRepository : IOrganizationRepository
{
    private readonly LocaGuestDbContext _context;

    public OrganizationRepository(LocaGuestDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Organization?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Email == email, cancellationToken);
    }

    public async Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .OrderBy(o => o.Number)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<Organization> Query()
    {
        return _context.Organizations.AsQueryable();
    }

    public void Add(Organization organization)
    {
        _context.Organizations.Add(organization);
    }

    public void Update(Organization organization)
    {
        _context.Organizations.Update(organization);
    }

    public void Delete(Organization organization)
    {
        _context.Organizations.Remove(organization);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .AnyAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .AnyAsync(o => o.Email == email, cancellationToken);
    }

    public async Task<int> GetLastNumberAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .OrderByDescending(o => o.Number)
            .Select(o => o.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> GetTenantNumberAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var organisation = await _context.Organizations
            .Where(o => o.Id == tenantId)
            .AsNoTracking()
            .Select(o => new { o.Code })
        .FirstOrDefaultAsync(cancellationToken);

        return organisation?.Code ?? throw new Exception($"Organisation with tenant id {tenantId} not found");
    }
}
