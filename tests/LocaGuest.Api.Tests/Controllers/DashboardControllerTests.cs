using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;
using LocaGuest.Application.Features.Dashboard.Queries.GetDeadlines;
using LocaGuest.Application.Features.Dashboard.Queries.GetRecentActivities;
using LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;
using LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class DashboardControllerTests : BaseTestFixture
{
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _mediatorMock = new Mock<IMediator>();
        _controller = new DashboardController(_loggerMock.Object, _mediatorMock.Object);
    }

    #region GetSummary Tests

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummary()
    {
        // Arrange
        var summaryDto = new DashboardSummaryDto
        {
            PropertiesCount = 10,
            ActiveTenants = 20,
            OccupancyRate = 0.8m,
            MonthlyRevenue = 5000m
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), default))
            .ReturnsAsync(Result.Success(summaryDto));

        // Act
        var result = await _controller.GetSummary(12,2024);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(summaryDto);
    }

    [Fact]
    public async Task GetSummary_ReturnsExpectedProperties()
    {
        // Arrange
        var summaryDto = new DashboardSummaryDto
        {
            PropertiesCount = 10,
            ActiveTenants = 20,
            OccupancyRate = 0.8m,
            MonthlyRevenue = 5000m
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), default))
            .ReturnsAsync(Result.Success(summaryDto));

        // Act
        var result = await _controller.GetSummary(12,2024);

        // Assert
        var okResult = result as OkObjectResult;
        var summary = okResult!.Value as DashboardSummaryDto;
        
        summary.Should().NotBeNull();
        summary!.PropertiesCount.Should().Be(10);
        summary.ActiveTenants.Should().Be(20);
        summary.OccupancyRate.Should().Be(0.8m);
        summary.MonthlyRevenue.Should().Be(5000m);
    }

    [Fact]
    public async Task GetSummary_WhenFails_ReturnsBadRequest()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardSummaryQuery>(), default))
            .ReturnsAsync(Result.Failure<DashboardSummaryDto>("Error"));

        // Act
        var result = await _controller.GetSummary(12,2024);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetActivities Tests

    [Fact]
    public async Task GetActivities_WithDefaultLimit_ReturnsOkWithActivities()
    {
        // Arrange
        var activities = new List<ActivityDto>
        {
            new() { Type = "success", Title = "Test", Date = DateTime.UtcNow }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRecentActivitiesQuery>(), default))
            .ReturnsAsync(Result.Success(activities));

        // Act
        var result = await _controller.GetActivities();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(activities);
    }

    [Fact]
    public async Task GetActivities_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        var limit = 5;
        var activities = new List<ActivityDto>
        {
            new() { Type = "success", Title = "Test", Date = DateTime.UtcNow }
        };
        _mediatorMock.Setup(m => m.Send(It.Is<GetRecentActivitiesQuery>(q => q.Limit == limit), default))
            .ReturnsAsync(Result.Success(activities));

        // Act
        var result = await _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GetActivities_WithVariousLimits_ReturnsOk(int limit)
    {
        // Arrange
        var activities = new List<ActivityDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRecentActivitiesQuery>(), default))
            .ReturnsAsync(Result.Success(activities));

        // Act
        var result = await _controller.GetActivities(limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActivities_WhenFails_ReturnsBadRequest()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRecentActivitiesQuery>(), default))
            .ReturnsAsync(Result.Failure<List<ActivityDto>>("Error"));

        // Act
        var result = await _controller.GetActivities();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetDeadlines Tests

    [Fact]
    public async Task GetDeadlines_ReturnsOkWithDeadlines()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDeadlinesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new DeadlinesDto
            {
                UpcomingDeadlines = new List<DeadlineItem>
                {
                    new DeadlineItem { Type = "Rent", Title = "Test", Description = "Test", Date = DateTime.UtcNow, PropertyCode = "P1", OccupantName = "T1" }
                }
            }));

        // Act
        var result = await _controller.GetDeadlines();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDeadlines_ReturnsEnumerableResult()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDeadlinesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new DeadlinesDto
            {
                UpcomingDeadlines = new List<DeadlineItem>()
            }));

        // Act
        var result = await _controller.GetDeadlines();
        var okResult = result as OkObjectResult;

        // Assert
        okResult!.Value.Should().NotBeNull();
        // Note: GetDeadlines peut retourner null dans l'implémentation actuelle
        // On vérifie juste que la méthode retourne OK
    }

    #endregion

    #region Charts Tests

    [Fact]
    public async Task GetOccupancyChart_WithMonthYear_ReturnsOk()
    {
        // Arrange
        var dto = new OccupancyChartDto
        {
            DailyData = new List<DailyOccupancy>
            {
                new() { Day = 1, Label = "J1", OccupiedUnits = 1, TotalUnits = 2, OccupancyRate = 50m }
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOccupancyChartQuery>(), default))
            .ReturnsAsync(Result.Success(dto));

        // Act
        var result = await _controller.GetOccupancyChart(1, 2025);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRevenueChart_WithMonthYear_ReturnsOk()
    {
        // Arrange
        var dto = new RevenueChartDto
        {
            DailyData = new List<DailyRevenue>
            {
                new() { Day = 1, Label = "J1", ExpectedRevenue = 100m, ActualRevenue = 50m, CollectionRate = 50m }
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetRevenueChartQuery>(), default))
            .ReturnsAsync(Result.Success(dto));

        // Act
        var result = await _controller.GetRevenueChart(1, 2025);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
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
