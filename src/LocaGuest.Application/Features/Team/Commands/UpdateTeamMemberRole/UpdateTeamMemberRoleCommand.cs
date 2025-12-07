using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Team.Commands.UpdateTeamMemberRole;

public class UpdateTeamMemberRoleCommand : IRequest<Result>
{
    public Guid TeamMemberId { get; set; }
    public string NewRole { get; set; } = string.Empty;
}
