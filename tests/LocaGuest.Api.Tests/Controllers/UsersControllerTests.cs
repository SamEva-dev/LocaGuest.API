using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using System.Net;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class UsersControllerTests : BaseTestFixture
{
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IAuthGateClient> _authGateClientMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _loggerMock = new Mock<ILogger<UsersController>>();
        _mediatorMock = new Mock<IMediator>();
        _authGateClientMock = new Mock<IAuthGateClient>();

        _controller = new UsersController(_loggerMock.Object, _mediatorMock.Object, _authGateClientMock.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_WhenAuthGateReturnsSuccess_ReturnsOkWithUsers()
    {
        // Arrange
        var users = new List<AuthGateUserDto>
        {
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            }
        };

        _authGateClientMock
            .Setup(c => c.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((HttpStatusCode.OK, (IReadOnlyList<AuthGateUserDto>?)users));

        // Act
        var result = await _controller.GetAllUsers(CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAllUsers_WhenAuthGateFails_ReturnsInternalServerError()
    {
        // Arrange
        _authGateClientMock
            .Setup(c => c.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act
        var result = await _controller.GetAllUsers(CancellationToken.None) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteUser Tests (GetUser doesn't exist - only GetAllUsers)

    [Fact]
    public async Task DeleteUser_WithExistingUserId_ReturnsNoContent_Version2()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _authGateClientMock
            .Setup(c => c.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HttpStatusCode.NoContent);

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None) as NoContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteUser_WithNonExistingUserId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _authGateClientMock
            .Setup(c => c.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HttpStatusCode.NotFound);

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteUser_WhenAuthGateFails_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _authGateClientMock
            .Setup(c => c.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
    }

    #endregion
}
