using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class TeamMemberRepository : Repository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<TeamMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TeamMember>()
            .Include(tm => tm.Organization)
            .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<IEnumerable<TeamMember>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TeamMember>()
            .Include(tm => tm.Organization)
            .Where(tm => tm.OrganizationId == organizationId)
            .OrderByDescending(tm => tm.InvitedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TeamMember>> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TeamMember>()
            .Include(tm => tm.Organization)
            .Where(tm => tm.OrganizationId == organizationId && tm.IsActive)
            .OrderByDescending(tm => tm.InvitedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TeamMember>()
            .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == organizationId && tm.IsActive, cancellationToken);
    }
}
