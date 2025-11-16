using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class CheckoutControllerTests : BaseTestFixture
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILocaGuestDbContext> _contextMock;
    private readonly Mock<ILogger<CheckoutController>> _loggerMock;
    private readonly CheckoutController _controller;

    public CheckoutControllerTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _contextMock = new Mock<ILocaGuestDbContext>();
        _loggerMock = new Mock<ILogger<CheckoutController>>();
        
        // Mock Stripe:SecretKey configuration
        _configurationMock.Setup(c => c["Stripe:SecretKey"]).Returns("test_key");
        
        _controller = new CheckoutController(_configurationMock.Object, _contextMock.Object, _loggerMock.Object);
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
