using FluentValidation;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateContract;

public sealed class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("PropertyId is required");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(t => t == "Furnished" || t == "Unfurnished")
            .WithMessage("Type must be 'Furnished' or 'Unfurnished'");

        RuleFor(x => x.StartDate)
            .Must(d => d > DateTime.MinValue).WithMessage("StartDate is required");

        RuleFor(x => x.EndDate)
            .Must(d => d > DateTime.MinValue).WithMessage("EndDate is required")
            .GreaterThan(x => x.StartDate).WithMessage("EndDate must be after StartDate");

        RuleFor(x => x.Rent)
            .GreaterThan(0).WithMessage("Rent must be positive");

        RuleFor(x => x.Charges)
            .GreaterThanOrEqualTo(0).WithMessage("Charges cannot be negative");

        RuleFor(x => x.Deposit)
            .GreaterThanOrEqualTo(0).When(x => x.Deposit.HasValue)
            .WithMessage("Deposit cannot be negative");

        RuleFor(x => x.PaymentDueDay)
            .InclusiveBetween(1, 31).WithMessage("PaymentDueDay must be between 1 and 31");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes != null)
            .WithMessage("Notes cannot exceed 1000 characters");
    }
}
