using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Team.Commands.InviteTeamMember;

public class InviteTeamMemberCommand : IRequest<Result<InviteTeamMemberResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class InviteTeamMemberResponse
{
    public Guid TeamMemberId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
}
