using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Provisioning.Invitations.Commands.ConsumeInvitation;

public sealed record ConsumeInvitationCommand(
    string Token,
    string UserId,
    string UserEmail,
    string IdempotencyKey) : IRequest<Result<ConsumeInvitationResponseDto>>;

public sealed record ConsumeInvitationResponseDto(
    Guid OrganizationId,
    Guid TeamMemberId,
    string Role);
