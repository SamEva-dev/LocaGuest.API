using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public sealed class InvitationRepository : Repository<Invitation>, IInvitationRepository
{
    public InvitationRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        IQueryable<Invitation> query = _context.Set<Invitation>();
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invitation?> GetPendingByOrganizationAndEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();

        return await _context.Set<Invitation>()
            .Where(i => i.OrganizationId == organizationId)
            .Where(i => i.Status == InvitationStatus.Pending)
            .Where(i => i.Email == normalizedEmail)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
