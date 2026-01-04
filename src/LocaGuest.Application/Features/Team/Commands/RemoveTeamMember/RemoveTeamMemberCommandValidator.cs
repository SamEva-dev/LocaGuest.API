using FluentValidation;

namespace LocaGuest.Application.Features.Team.Commands.RemoveTeamMember;

public sealed class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamMemberId)
            .NotEmpty().WithMessage("TeamMemberId is required");
    }
}
