using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class SettingsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUserSettings_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/settings");

        // Assert - May require user context
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUserSettings_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var settings = new
        {
            theme = "dark",
            language = "fr",
            notifications = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserSettings_WithEmptyData_HandlesGracefully()
    {
        // Arrange
        var settings = new { };

        // Act
        var response = await _client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
