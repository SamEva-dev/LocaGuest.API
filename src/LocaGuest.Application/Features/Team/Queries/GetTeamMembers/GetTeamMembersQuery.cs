using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Team.Queries.GetTeamMembers;

public class GetTeamMembersQuery : IRequest<Result<List<TeamMemberDto>>>
{
    public bool ActiveOnly { get; set; } = true;
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFirstName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public bool IsActive { get; set; }
}
