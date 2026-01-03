using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Team.Commands.UpdateTeamMemberRole;

public class UpdateTeamMemberRoleCommandHandler : IRequestHandler<UpdateTeamMemberRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<UpdateTeamMemberRoleCommandHandler> _logger;

    public UpdateTeamMemberRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<UpdateTeamMemberRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTeamMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // Valider le nouveau rôle
        if (!TeamRoles.IsValid(request.NewRole))
        {
            return Result.Failure($"Invalid role: {request.NewRole}");
        }

        var organizationId = _orgContext.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            return Result.Failure("Organization context not found");
        }

        var teamMember = await _unitOfWork.TeamMembers.GetByIdAsync(request.TeamMemberId, cancellationToken);
        if (teamMember == null)
        {
            return Result.Failure("Team member not found");
        }

        // Vérifier que le membre appartient à l'organization
        if (teamMember.OrganizationId != organizationId.Value)
        {
            return Result.Failure("Unauthorized access");
        }

        // Ne pas permettre la modification du rôle Owner
        if (teamMember.Role == TeamRoles.Owner || request.NewRole == TeamRoles.Owner)
        {
            return Result.Failure("Cannot modify owner role");
        }

        teamMember.UpdateRole(request.NewRole);
        _unitOfWork.TeamMembers.Update(teamMember);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Team member {TeamMemberId} role updated to {NewRole}", 
            request.TeamMemberId, request.NewRole);

        return Result.Success();
    }
}
