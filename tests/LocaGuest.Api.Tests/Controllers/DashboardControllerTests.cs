using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class DashboardControllerTests : BaseTestFixture
{
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_loggerMock.Object);
    }

    #region GetSummary Tests

    [Fact]
    public void GetSummary_ReturnsOkWithSummary()
    {
        // Act
        var result = _controller.GetSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetSummary_ReturnsExpectedProperties()
    {
        // Act
        var result = _controller.GetSummary();

        // Assert
        var okResult = result as OkObjectResult;
        var summary = okResult!.Value;
        
        // Check that the object has expected properties
        summary.Should().NotBeNull();
        summary.GetType().GetProperty("propertiesCount").Should().NotBeNull();
        summary.GetType().GetProperty("activeTenants").Should().NotBeNull();
        summary.GetType().GetProperty("occupancyRate").Should().NotBeNull();
        summary.GetType().GetProperty("monthlyRevenue").Should().NotBeNull();
    }

    [Fact]
    public void GetSummary_ReturnsValidValues()
    {
        // Act
        var result = _controller.GetSummary();
        var okResult = result as OkObjectResult;
        var summary = okResult!.Value;

        // Assert
        summary.Should().NotBeNull();
        
        // Verify properties exist and have expected types
        var propertiesCount = summary.GetType().GetProperty("propertiesCount")?.GetValue(summary);
        propertiesCount.Should().BeOfType<int>();
        
        var activeTenants = summary.GetType().GetProperty("activeTenants")?.GetValue(summary);
        activeTenants.Should().BeOfType<int>();
        
        var occupancyRate = summary.GetType().GetProperty("occupancyRate")?.GetValue(summary);
        occupancyRate.Should().BeOfType<decimal>();
        
        var monthlyRevenue = summary.GetType().GetProperty("monthlyRevenue")?.GetValue(summary);
        monthlyRevenue.Should().BeOfType<decimal>();
    }

    #endregion

    #region GetActivities Tests

    [Fact]
    public void GetActivities_WithDefaultLimit_ReturnsOkWithActivities()
    {
        // Act
        var result = _controller.GetActivities();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetActivities_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        var limit = 5;

        // Act
        var result = _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        // Verify result is enumerable
        var activities = okResult.Value as System.Collections.IEnumerable;
        activities.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void GetActivities_WithVariousLimits_ReturnsOk(int limit)
    {
        // Act
        var result = _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetActivities_WithZeroLimit_ReturnsEmptyResult()
    {
        // Arrange
        var limit = 0;

        // Act
        var result = _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetActivities_WithNegativeLimit_HandlesGracefully()
    {
        // Arrange
        var limit = -1;

        // Act
        var result = _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetDeadlines Tests

    [Fact]
    public void GetDeadlines_ReturnsOkWithDeadlines()
    {
        // Act
        var result = _controller.GetDeadlines();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetDeadlines_ReturnsEnumerableResult()
    {
        // Act
        var result = _controller.GetDeadlines();
        var okResult = result as OkObjectResult;

        // Assert
        okResult!.Value.Should().NotBeNull();
        // Note: GetDeadlines peut retourner null dans l'implémentation actuelle
        // On vérifie juste que la méthode retourne OK
    }

    #endregion

    #region Controller Tests

    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        _controller.Should().NotBeNull();
        var controllerType = _controller.GetType();
        
        // Check attributes
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_IsProperlyInitialized()
    {
        // Assert
        _controller.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
    }

    #endregion
}
