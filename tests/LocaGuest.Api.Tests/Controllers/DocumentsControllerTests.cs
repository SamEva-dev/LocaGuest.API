using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class DocumentsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<DocumentsController>> _loggerMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_mediatorMock.Object, _loggerMock.Object);
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

    // TODO: Add GenerateDocument test when DTO is properly referenced

    [Fact]
    public async Task GetRecentDocuments_ReturnsOkWithDocuments()
    {
        // Act
        var result = await _controller.GetRecentDocuments();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }
}
