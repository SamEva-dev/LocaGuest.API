using FluentValidation;
using LocaGuest.Domain.Entities;

namespace LocaGuest.Application.Features.Team.Commands.UpdateTeamMemberRole;

public sealed class UpdateTeamMemberRoleCommandValidator : AbstractValidator<UpdateTeamMemberRoleCommand>
{
    public UpdateTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.TeamMemberId)
            .NotEmpty().WithMessage("TeamMemberId is required");

        RuleFor(x => x.NewRole)
            .NotEmpty().WithMessage("NewRole is required")
            .Must(TeamRoles.IsValid)
            .WithMessage("NewRole is invalid");
    }
}
