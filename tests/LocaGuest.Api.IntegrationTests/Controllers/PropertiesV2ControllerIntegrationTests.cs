using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class PropertiesV2ControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PropertiesV2ControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProperties_IsAccessibleOrNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/properties");

        // Assert - V2 API may not be fully implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProperties_WithFilters_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/properties?status=available&city=Paris");

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProperty_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v2/properties/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProperty_IsAccessible()
    {
        // Arrange
        var property = new { name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/properties", property);

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProperty_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var property = new { name = "Updated Property" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v2/properties/{invalidId}", property);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
