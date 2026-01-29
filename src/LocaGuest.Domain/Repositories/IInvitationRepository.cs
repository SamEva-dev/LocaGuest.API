using LocaGuest.Domain.Entities;

namespace LocaGuest.Domain.Repositories;

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false);

    Task<Invitation?> GetPendingByOrganizationAndEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default);
}
