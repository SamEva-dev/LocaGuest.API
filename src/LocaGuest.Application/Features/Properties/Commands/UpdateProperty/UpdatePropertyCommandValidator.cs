using FluentValidation;

namespace LocaGuest.Application.Features.Properties.Commands.UpdateProperty;

public sealed class UpdatePropertyCommandValidator : AbstractValidator<UpdatePropertyCommand>
{
    private static readonly string[] AllowedUsageTypes = new[] { "Complete", "Colocation", "Airbnb" };

    public UpdatePropertyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(300)
            .When(x => x.Address is not null);

        // DB invariant: City is required; if you update City, it cannot be empty.
        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.City is not null);

        RuleFor(x => x.Surface)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Surface.HasValue);

        RuleFor(x => x.Rent)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Rent.HasValue);

        RuleFor(x => x.Charges)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Charges.HasValue);

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PurchasePrice.HasValue);

        RuleFor(x => x.PropertyUsageType)
            .Must(v => v is null || AllowedUsageTypes.Contains(v))
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
