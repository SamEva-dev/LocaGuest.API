using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class SettingsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SettingsController>>();
        _controller = new SettingsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        _controller.Should().NotBeNull();
        var controllerType = _controller.GetType();
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_IsProperlyInitialized()
    {
        // Assert
        _controller.Should().NotBeNull();
        _mediatorMock.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
    }

    [Fact]
    public void GetUserSettings_MethodExists()
    {
        // Arrange
        var method = typeof(SettingsController).GetMethod("GetUserSettings");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void UpdateUserSettings_MethodExists()
    {
        // Arrange
        var method = typeof(SettingsController).GetMethod("UpdateUserSettings");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
