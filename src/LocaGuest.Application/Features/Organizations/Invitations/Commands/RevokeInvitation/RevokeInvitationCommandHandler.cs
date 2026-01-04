using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using MediatR;

namespace LocaGuest.Application.Features.Organizations.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler : IRequestHandler<RevokeInvitationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ICurrentUserService _currentUser;

    public RevokeInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        var organizationId = _orgContext.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
            return Result.Failure("Organization context not found");

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
            return Result.Failure("User context not found");

        var inviter = await _unitOfWork.TeamMembers.GetByUserAndOrganizationAsync(userId.Value, organizationId.Value, cancellationToken);
        if (inviter == null)
            return Result.Failure("You are not a member of this organization");

        if (inviter.Role is not (TeamRoles.Owner or TeamRoles.Admin))
            return Result.Failure("You are not allowed to revoke invitations");

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation == null)
            return Result.Failure("Invitation not found");

        if (invitation.OrganizationId != organizationId.Value)
            return Result.Failure("Invitation not in current organization");

        invitation.Revoke(DateTime.UtcNow);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
