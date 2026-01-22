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
    private readonly ILogger<InviteTeamMemberCommandHandler> _logger;

    public InviteTeamMemberCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ICurrentUserService currentUserService,
        ILogger<InviteTeamMemberCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
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

        return Result.Failure<InviteTeamMemberResponse>(
            "Invitations are managed by AuthGate. Use AuthGate /api/auth/invite to invite collaborators.");
    }
}
