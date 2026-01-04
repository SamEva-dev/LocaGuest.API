using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class StripeWebhookControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<StripeWebhookController>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly StripeWebhookController _controller;

    public StripeWebhookControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<StripeWebhookController>>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Mock Stripe:WebhookSecret configuration
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("test_secret");
        
        _controller = new StripeWebhookController(_mediatorMock.Object, _loggerMock.Object, _configurationMock.Object);
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
        _mediatorMock.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
        _configurationMock.Should().NotBeNull();
    }

    [Fact]
    public void HandleWebhook_MethodExists()
    {
        // Arrange
        var method = typeof(StripeWebhookController).GetMethod("HandleWebhook");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
