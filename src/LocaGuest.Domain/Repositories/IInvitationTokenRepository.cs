using LocaGuest.Domain.Entities;

namespace LocaGuest.Domain.Repositories;

public interface IInvitationTokenRepository : IRepository<InvitationToken>
{
    Task<InvitationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<InvitationToken?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default);
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}
