using FluentValidation;

namespace LocaGuest.Application.Features.Properties.Commands.CreateProperty;

public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    private static readonly string[] AllowedUsageTypes = new[] { "Complete", "Colocation", "Airbnb" };

    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .NotEmpty();

        RuleFor(x => x.Surface)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Rent)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Charges)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Charges.HasValue);

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PurchasePrice.HasValue);

        RuleFor(x => x.PropertyUsageType)
            .NotEmpty()
            .Must(v => AllowedUsageTypes.Contains(v))
            .WithMessage("PropertyUsageType must be one of: Complete, Colocation, Airbnb");

        When(x => string.Equals(x.PropertyUsageType, "Colocation", StringComparison.Ordinal), () =>
        {
            RuleFor(x => x.TotalRooms)
                .NotNull()
                .GreaterThan(0);
        });

        When(x => string.Equals(x.PropertyUsageType, "Airbnb", StringComparison.Ordinal), () =>
        {
            RuleFor(x => x.PricePerNight)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.MinimumStay)
                .GreaterThan(0)
                .When(x => x.MinimumStay.HasValue);

            RuleFor(x => x.MaximumStay)
                .GreaterThan(0)
                .When(x => x.MaximumStay.HasValue);

            RuleFor(x => x)
                .Must(x => !x.MinimumStay.HasValue || !x.MaximumStay.HasValue || x.MinimumStay.Value <= x.MaximumStay.Value)
                .WithMessage("MinimumStay must be <= MaximumStay");
        });
    }
}
