using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class RentabilityScenariosControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly RentabilityScenariosController _controller;

    public RentabilityScenariosControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new RentabilityScenariosController(_mediatorMock.Object);
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
    }

    [Fact]
    public void GetUserScenarios_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("GetUserScenarios");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void SaveScenario_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("SaveScenario");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void CloneScenario_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("CloneScenario");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void DeleteScenario_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("DeleteScenario");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetScenarioVersions_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("GetScenarioVersions");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void RestoreVersion_MethodExists()
    {
        // Arrange
        var method = typeof(RentabilityScenariosController).GetMethod("RestoreVersion");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
