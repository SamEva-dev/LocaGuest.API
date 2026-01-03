using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Team.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<RemoveTeamMemberCommandHandler> _logger;

    public RemoveTeamMemberCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<RemoveTeamMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
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

        // Ne pas permettre la suppression du Owner
        if (teamMember.Role == TeamRoles.Owner)
        {
            return Result.Failure("Cannot remove the owner");
        }

        teamMember.Remove();
        _unitOfWork.TeamMembers.Update(teamMember);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Team member {TeamMemberId} removed from organization {OrganizationId}", 
            request.TeamMemberId, organizationId.Value);

        return Result.Success();
    }
}
