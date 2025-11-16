using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class AnalyticsControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(AnalyticsController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void GetProfitabilityStats_MethodExists()
    {
        // Arrange
        var method = typeof(AnalyticsController).GetMethod("GetProfitabilityStats");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetRevenueEvolution_MethodExists()
    {
        // Arrange
        var method = typeof(AnalyticsController).GetMethod("GetRevenueEvolution");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetPropertyPerformance_MethodExists()
    {
        // Arrange
        var method = typeof(AnalyticsController).GetMethod("GetPropertyPerformance");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetAvailableYears_MethodExists()
    {
        // Arrange
        var method = typeof(AnalyticsController).GetMethod("GetAvailableYears");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
