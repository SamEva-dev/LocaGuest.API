using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Team.Queries.GetTeamMembers;

public class GetTeamMembersQueryHandler : IRequestHandler<GetTeamMembersQuery, Result<List<TeamMemberDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<GetTeamMembersQueryHandler> _logger;

    public GetTeamMembersQueryHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<GetTeamMembersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result<List<TeamMemberDto>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var organizationId = _orgContext.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            return Result.Failure<List<TeamMemberDto>>("Organization context not found");
        }

        var teamMembers = request.ActiveOnly
            ? await _unitOfWork.TeamMembers.GetActiveByOrganizationAsync(organizationId.Value, cancellationToken)
            : await _unitOfWork.TeamMembers.GetByOrganizationAsync(organizationId.Value, cancellationToken);

        var dtos = teamMembers.Select(tm => new TeamMemberDto
        {
            Id = tm.Id,
            UserId = tm.UserId,
            UserEmail = tm.UserEmail,
            UserFirstName = "", // TODO: Fetch from auth service
            UserLastName = "",  // TODO: Fetch from auth service
            Role = tm.Role,
            InvitedAt = tm.InvitedAt,
            AcceptedAt = tm.AcceptedAt,
            IsActive = tm.IsActive
        }).ToList();

        return Result.Success(dtos);
    }
}
