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
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InviteTeamMemberCommandHandler> _logger;

    public InviteTeamMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ILogger<InviteTeamMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
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
        var organizationId = _tenantContext.TenantId;
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
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Team member {UserId} invited to organization {OrganizationId} with role {Role}", 
            userId, organizationId.Value, request.Role);

        // TODO: Envoyer email d'invitation avec token
        // await _emailService.SendTeamInvitationEmail(request.Email, teamMember.Id, tenantName);

        return Result.Success(new InviteTeamMemberResponse
        {
            TeamMemberId = teamMember.Id,
            Email = request.Email,
            Role = teamMember.Role,
            InvitedAt = teamMember.InvitedAt
        });
    }
}
