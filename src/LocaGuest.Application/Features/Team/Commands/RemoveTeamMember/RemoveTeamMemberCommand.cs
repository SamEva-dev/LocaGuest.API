using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Team.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommand : IRequest<Result>
{
    public Guid TeamMemberId { get; set; }
}
