using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class UsersControllerTests : BaseTestFixture
{
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _loggerMock = new Mock<ILogger<UsersController>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("AuthGateApi"))
            .Returns(httpClient);

        _controller = new UsersController(_loggerMock.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_WhenAuthGateReturnsSuccess_ReturnsOkWithUsers()
    {
        // Arrange
        var usersJson = "[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"test@test.com\",\"firstName\":\"Test\",\"lastName\":\"User\"}]";
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains("/api/users")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(usersJson, System.Text.Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _controller.GetAllUsers(_httpClientFactoryMock.Object) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAllUsers_WhenAuthGateFails_ReturnsInternalServerError()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act
        var result = await _controller.GetAllUsers(_httpClientFactoryMock.Object) as ObjectResult;

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

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Delete && 
                    req.RequestUri!.ToString().Contains($"/api/users/{userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _controller.DeleteUser(userId, _httpClientFactoryMock.Object) as NoContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteUser_WithNonExistingUserId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/api/users/{userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _controller.DeleteUser(userId, _httpClientFactoryMock.Object) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteUser_WhenAuthGateFails_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act
        var result = await _controller.DeleteUser(userId, _httpClientFactoryMock.Object) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
    }

    #endregion
}
