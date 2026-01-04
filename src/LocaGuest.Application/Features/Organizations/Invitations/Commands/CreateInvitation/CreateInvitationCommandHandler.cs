using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Invitations.Commands.CreateInvitation;

public sealed class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, Result<CreateInvitationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateInvitationCommandHandler> _logger;

    public CreateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ICurrentUserService currentUser,
        ILogger<CreateInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<CreateInvitationResponse>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        var organizationId = _orgContext.OrganizationId;
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
            return Result.Failure<CreateInvitationResponse>("Organization context not found");

        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
            return Result.Failure<CreateInvitationResponse>("User context not found");

        var inviter = await _unitOfWork.TeamMembers.GetByUserAndOrganizationAsync(userId.Value, organizationId.Value, cancellationToken);
        if (inviter == null)
            return Result.Failure<CreateInvitationResponse>("You are not a member of this organization");

        if (inviter.Role is not (TeamRoles.Owner or TeamRoles.Admin))
            return Result.Failure<CreateInvitationResponse>("You are not allowed to invite users");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure<CreateInvitationResponse>("Email is required");

        var email = request.Email.Trim().ToLowerInvariant();

        var role = string.IsNullOrWhiteSpace(request.Role) ? TeamRoles.Occupant : request.Role;
        if (!TeamRoles.IsValid(role))
            return Result.Failure<CreateInvitationResponse>($"Invalid role: {role}");

        var existingPending = await _unitOfWork.Invitations.GetPendingByOrganizationAndEmailAsync(organizationId.Value, email, cancellationToken);
        if (existingPending != null)
        {
            existingPending.Revoke(DateTime.UtcNow);
        }

        var ttl = TimeSpan.FromHours(72);
        var invitation = Invitation.Create(
            organizationId: organizationId.Value,
            email: email,
            role: role,
            createdByUserId: userId.Value,
            ttl: ttl,
            token: out var token);

        await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Invitation created {InvitationId} for {Email} in org {OrganizationId}", invitation.Id, email, organizationId.Value);

        return Result.Success(new CreateInvitationResponse
        {
            InvitationId = invitation.Id,
            Token = token,
            ExpiresAtUtc = invitation.ExpiresAtUtc
        });
    }
}
