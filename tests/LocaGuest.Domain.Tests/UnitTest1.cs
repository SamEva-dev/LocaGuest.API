using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Tests;

public class UnitTest1
{
    [Fact]
    public void Property_Create_WithNegativeRent_Throws()
    {
        Assert.Throws<ValidationException>(() => Property.Create(
            name: "Test",
            address: "1 rue test",
            city: "Paris",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Complete,
            rent: -1m,
            bedrooms: 1,
            bathrooms: 1));
    }

    [Fact]
    public void Property_SetAirbnbSettings_WithInvalidStayDuration_Throws()
    {
        var p = Property.Create(
            name: "Test",
            address: "1 rue test",
            city: "Paris",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Airbnb,
            rent: 100m,
            bedrooms: 1,
            bathrooms: 1);

        Assert.Throws<ValidationException>(() =>
            p.SetAirbnbSettings(minimumStay: 10, maximumStay: 5, pricePerNight: 50m));
    }
}
