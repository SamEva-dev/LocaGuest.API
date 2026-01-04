using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Invitations.Commands.ConsumeInvitation;

public sealed class ConsumeInvitationCommandHandler : IRequestHandler<ConsumeInvitationCommand, Result<ConsumeInvitationResponseDto>>
{
    private readonly IInvitationProvisioningService _provisioning;
    private readonly ILogger<ConsumeInvitationCommandHandler> _logger;

    public ConsumeInvitationCommandHandler(
        IInvitationProvisioningService provisioning,
        ILogger<ConsumeInvitationCommandHandler> logger)
    {
        _provisioning = provisioning;
        _logger = logger;
    }

    public async Task<Result<ConsumeInvitationResponseDto>> Handle(ConsumeInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _provisioning.ConsumeInvitationAsync(
                new ConsumeInvitationRequest(
                    Token: request.Token,
                    UserId: request.UserId,
                    UserEmail: request.UserEmail),
                request.IdempotencyKey,
                cancellationToken);

            return Result.Success(new ConsumeInvitationResponseDto(
                OrganizationId: result.OrganizationId,
                TeamMemberId: result.TeamMemberId,
                Role: result.Role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provisioning consume invitation failed");
            return Result.Failure<ConsumeInvitationResponseDto>($"Failed to consume invitation: {ex.Message}");
        }
    }
}
