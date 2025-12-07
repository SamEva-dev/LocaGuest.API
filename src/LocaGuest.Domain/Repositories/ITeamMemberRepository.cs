using LocaGuest.Domain.Entities;

namespace LocaGuest.Domain.Repositories;

public interface ITeamMemberRepository : IRepository<TeamMember>
{
    Task<TeamMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamMember>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamMember>> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
}
