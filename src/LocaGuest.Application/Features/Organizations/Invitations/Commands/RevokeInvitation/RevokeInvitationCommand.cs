using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommand : IRequest<Result>
{
    public Guid InvitationId { get; set; }
}
