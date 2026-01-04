using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class SubscriptionsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SubscriptionsController _controller;

    public SubscriptionsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new SubscriptionsController(_mediatorMock.Object);
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
    public void GetPlans_MethodExists()
    {
        // Arrange
        var method = typeof(SubscriptionsController).GetMethod("GetPlans");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetPlans_HasAllowAnonymousAttribute()
    {
        // Arrange
        var method = typeof(SubscriptionsController).GetMethod("GetPlans");

        // Assert
        method.Should().NotBeNull();
        var allowAnonymousAttr = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false)
            .FirstOrDefault();
        allowAnonymousAttr.Should().NotBeNull();
    }
}
