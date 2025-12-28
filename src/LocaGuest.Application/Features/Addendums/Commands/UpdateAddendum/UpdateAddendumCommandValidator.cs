using FluentValidation;

namespace LocaGuest.Application.Features.Addendums.Commands.UpdateAddendum;

public class UpdateAddendumCommandValidator : AbstractValidator<UpdateAddendumCommand>
{
    public UpdateAddendumCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Addendum ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).When(x => x.Reason != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description != null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes != null);
    }
}
