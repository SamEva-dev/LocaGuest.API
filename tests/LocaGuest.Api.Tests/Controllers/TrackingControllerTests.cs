using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class TrackingControllerTests : BaseTestFixture
{
    private readonly Mock<ITrackingService> _trackingServiceMock;
    private readonly Mock<ILogger<TrackingController>> _loggerMock;
    private readonly TrackingController _controller;

    public TrackingControllerTests()
    {
        _trackingServiceMock = new Mock<ITrackingService>();
        _loggerMock = new Mock<ILogger<TrackingController>>();
        _controller = new TrackingController(_trackingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        _controller.Should().NotBeNull();
        var controllerType = _controller.GetType();
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_IsProperlyInitialized()
    {
        // Assert
        _controller.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
    }
}
