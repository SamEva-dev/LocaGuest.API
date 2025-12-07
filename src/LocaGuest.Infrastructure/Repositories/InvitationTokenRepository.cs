using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class InvitationTokenRepository : Repository<InvitationToken>, IInvitationTokenRepository
{
    public InvitationTokenRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<InvitationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Set<InvitationToken>()
            .Include(it => it.TeamMember)
            .FirstOrDefaultAsync(it => it.Token == token, cancellationToken);
    }

    public async Task<InvitationToken?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<InvitationToken>()
            .FirstOrDefaultAsync(it => it.TeamMemberId == teamMemberId && !it.IsUsed, cancellationToken);
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _context.Set<InvitationToken>()
            .Where(it => it.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.Set<InvitationToken>().RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
