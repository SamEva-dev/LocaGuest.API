using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class SubscriptionsControllerTests : BaseTestFixture
{
    private readonly Mock<ILocaGuestDbContext> _contextMock;
    private readonly Mock<ISubscriptionService> _subscriptionServiceMock;
    private readonly SubscriptionsController _controller;

    public SubscriptionsControllerTests()
    {
        _contextMock = new Mock<ILocaGuestDbContext>();
        _subscriptionServiceMock = new Mock<ISubscriptionService>();
        _controller = new SubscriptionsController(_contextMock.Object, _subscriptionServiceMock.Object);
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
        _contextMock.Should().NotBeNull();
        _subscriptionServiceMock.Should().NotBeNull();
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
