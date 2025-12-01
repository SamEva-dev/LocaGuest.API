using FluentValidation;

namespace LocaGuest.Application.Features.Contracts.Commands.RenewContract;

public class RenewContractCommandValidator : AbstractValidator<RenewContractCommand>
{
    public RenewContractCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("Contract ID is required");

        RuleFor(x => x.NewStartDate)
            .NotEmpty().WithMessage("New start date is required")
            .Must(date => date > DateTime.MinValue).WithMessage("Invalid start date");

        RuleFor(x => x.NewEndDate)
            .NotEmpty().WithMessage("New end date is required")
            .GreaterThan(x => x.NewStartDate).WithMessage("End date must be after start date");

        RuleFor(x => x.ContractType)
            .NotEmpty().WithMessage("Contract type is required")
            .Must(type => type == "Furnished" || type == "Unfurnished")
            .WithMessage("Contract type must be 'Furnished' or 'Unfurnished'");

        RuleFor(x => x.NewRent)
            .GreaterThan(0).WithMessage("Rent must be positive");

        RuleFor(x => x.NewCharges)
            .GreaterThanOrEqualTo(0).WithMessage("Charges cannot be negative");

        RuleFor(x => x.CustomClauses)
            .MaximumLength(2000).WithMessage("Custom clauses cannot exceed 2000 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");

        // Validation IRL si fournis
        When(x => x.PreviousIRL.HasValue || x.CurrentIRL.HasValue, () =>
        {
            RuleFor(x => x.PreviousIRL)
                .NotNull().WithMessage("Previous IRL is required when calculating rent revision")
                .GreaterThan(0).WithMessage("Previous IRL must be positive");

            RuleFor(x => x.CurrentIRL)
                .NotNull().WithMessage("Current IRL is required when calculating rent revision")
                .GreaterThan(0).WithMessage("Current IRL must be positive");
        });
    }
}
