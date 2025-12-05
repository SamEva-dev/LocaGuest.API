using FluentValidation;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;

public class CreateAddendumCommandValidator : AbstractValidator<CreateAddendumCommand>
{
    public CreateAddendumCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Contract ID is required");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Addendum type is required")
            .Must(type => new[] { "Financial", "Duration", "Occupants", "Clauses", "Free" }.Contains(type))
            .WithMessage("Invalid addendum type");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}
