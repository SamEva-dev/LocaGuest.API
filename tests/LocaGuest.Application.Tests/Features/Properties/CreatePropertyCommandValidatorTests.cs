using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Properties;

public class CreatePropertyCommandValidatorTests
{
    [Fact]
    public void CreatePropertyCommand_WhenCityEmpty_IsInvalid()
    {
        var validator = new CreatePropertyCommandValidator();
        var cmd = new CreatePropertyCommand
        {
            Name = "Test",
            Address = "1 rue test",
            City = "",
            Type = "Apartment",
            Rent = 100,
            Surface = 10,
            HasElevator = false,
            HasParking = false,
            HasBalcony = false,
            PropertyUsageType = "Complete"
        };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreatePropertyCommand_WhenAirbnbWithoutPricePerNight_IsInvalid()
    {
        var validator = new CreatePropertyCommandValidator();
        var cmd = new CreatePropertyCommand
        {
            Name = "Test",
            Address = "1 rue test",
            City = "Paris",
            Type = "Apartment",
            Rent = 100,
            Surface = 10,
            HasElevator = false,
            HasParking = false,
            HasBalcony = false,
            PropertyUsageType = "Airbnb",
            MinimumStay = 1,
            MaximumStay = 10
        };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
    }
}
