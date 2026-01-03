using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Team.Commands.InviteTeamMember;

public class InviteTeamMemberCommandHandler : IRequestHandler<InviteTeamMemberCommand, Result<InviteTeamMemberResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InviteTeamMemberCommandHandler> _logger;

    public InviteTeamMemberCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        ILogger<InviteTeamMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<InviteTeamMemberResponse>> Handle(InviteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        // Valider le rôle
        if (!TeamRoles.IsValid(request.Role))
        {
            return Result.Failure<InviteTeamMemberResponse>($"Invalid role: {request.Role}");
        }

        // Récupérer l'organization actuelle (tenant multi-tenant)
        var organizationId = _orgContext.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            return Result.Failure<InviteTeamMemberResponse>("Organization context not found");
        }

        // TODO: Chercher l'utilisateur par email (nécessite un UserRepository)
        // Pour l'instant, on va simuler que l'utilisateur existe
        // Dans une vraie implémentation, il faudrait :
        // 1. Chercher l'utilisateur par email
        // 2. Si l'utilisateur n'existe pas, créer un compte "pending" et envoyer email d'invitation
        // 3. Si l'utilisateur existe, l'ajouter directement et envoyer email de notification

        // Simulons un userId (à remplacer par la vraie logique)
        var userId = Guid.NewGuid(); // TODO: Récupérer depuis UserRepository

        // Vérifier si l'utilisateur n'est pas déjà membre de l'organization
        var existingMember = await _unitOfWork.TeamMembers.GetByUserAndOrganizationAsync(userId, organizationId.Value, cancellationToken);
        if (existingMember != null)
        {
            return Result.Failure<InviteTeamMemberResponse>("User is already a member of this organization");
        }

        // Créer le membre d'équipe
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var teamMember = new TeamMember(
            userId: userId,
            organizationId: organizationId.Value,
            role: request.Role,
            userEmail: request.Email,
            invitedBy: currentUserId
        );

        await _unitOfWork.TeamMembers.AddAsync(teamMember, cancellationToken);

        // Créer un token d'invitation sécurisé
        var invitationToken = new InvitationToken(
            teamMemberId: teamMember.Id,
            email: request.Email,
            organizationId: organizationId.Value,
            expirationHours: 72 // 3 jours
        );

        await _unitOfWork.InvitationTokens.AddAsync(invitationToken, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Team member {UserId} invited to organization {OrganizationId} with role {Role}", 
            userId, organizationId.Value, request.Role);

        // Récupérer l'organization pour le nom
        var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId.Value, cancellationToken);
        var organizationName = organization?.Name ?? "LocaGuest";
        var inviterName = _currentUserService.UserEmail ?? "Administrateur";

        // Envoyer email d'invitation avec token
        try
        {
            await _emailService.SendTeamInvitationEmailAsync(
                toEmail: request.Email,
                invitationToken: invitationToken.Token,
                organizationName: organizationName,
                inviterName: inviterName,
                role: request.Role,
                cancellationToken: cancellationToken
            );
            
            _logger.LogInformation("Invitation email sent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", request.Email);
            // Continue - l'invitation est créée même si l'email échoue
        }

        return Result.Success(new InviteTeamMemberResponse
        {
            TeamMemberId = teamMember.Id,
            Email = request.Email,
            Role = teamMember.Role,
            InvitedAt = teamMember.InvitedAt
        });
    }
}
