using LocaGuest.Application.Common;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Team.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AcceptInvitationResponse>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        // Récupérer le token
        var invitationToken = await _unitOfWork.InvitationTokens.GetByTokenAsync(request.Token, cancellationToken);
        if (invitationToken == null)
        {
            return Result.Failure<AcceptInvitationResponse>("Invalid or unknown invitation token");
        }

        // Vérifier que le token est valide
        if (!invitationToken.IsValid())
        {
            if (invitationToken.IsUsed)
            {
                return Result.Failure<AcceptInvitationResponse>("This invitation has already been used");
            }
            if (invitationToken.IsExpired())
            {
                return Result.Failure<AcceptInvitationResponse>("This invitation has expired");
            }
            return Result.Failure<AcceptInvitationResponse>("Invalid invitation token");
        }

        // Récupérer le team member
        var teamMember = await _unitOfWork.TeamMembers.GetByIdAsync(invitationToken.TeamMemberId, cancellationToken);
        if (teamMember == null)
        {
            return Result.Failure<AcceptInvitationResponse>("Team member not found");
        }

        // Marquer l'invitation comme acceptée
        teamMember.AcceptInvitation();
        invitationToken.MarkAsUsed();

        _unitOfWork.TeamMembers.Update(teamMember);
        _unitOfWork.InvitationTokens.Update(invitationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Invitation accepted for team member {TeamMemberId} in organization {OrganizationId}", 
            teamMember.Id, teamMember.OrganizationId);

        // Récupérer l'organization
        var organization = await _unitOfWork.Organizations.GetByIdAsync(teamMember.OrganizationId, cancellationToken);
        var organizationName = organization?.Name ?? "LocaGuest";

        // Envoyer email de notification à l'inviteur (si nécessaire)
        if (teamMember.InvitedBy.HasValue && teamMember.InvitedBy.Value != Guid.Empty)
        {
            // TODO: Récupérer l'email de l'inviteur depuis auth service
            // await _emailService.SendTeamInvitationAcceptedEmailAsync(inviterEmail, teamMember.UserEmail, organizationName, cancellationToken);
        }

        return Result.Success(new AcceptInvitationResponse
        {
            TeamMemberId = teamMember.Id,
            OrganizationId = teamMember.OrganizationId,
            OrganizationName = organizationName,
            Role = teamMember.Role,
            Email = teamMember.UserEmail
        });
    }
}
