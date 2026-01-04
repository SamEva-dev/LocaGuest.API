using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Invitations.Commands.CreateInvitation;

public sealed class CreateInvitationCommand : IRequest<Result<CreateInvitationResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class CreateInvitationResponse
{
    public Guid InvitationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
