using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Team.Commands.AcceptInvitation;

public class AcceptInvitationCommand : IRequest<Result<AcceptInvitationResponse>>
{
    public string Token { get; set; } = string.Empty;
}

public class AcceptInvitationResponse
{
    public Guid TeamMemberId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
