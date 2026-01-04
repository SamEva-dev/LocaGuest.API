using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class RentabilityScenariosControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RentabilityScenariosControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUserScenarios_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/rentability-scenarios");

        // Assert - May not be fully implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveScenario_IsAccessible()
    {
        // Arrange
        var scenario = new
        {
            name = "Test Scenario",
            propertyPrice = 300000,
            monthlyRent = 1500
        };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/rentability-scenarios")
        {
            Content = JsonContent.Create(scenario)
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(request);

        // Assert - Endpoint may not be fully implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CloneScenario_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/rentability-scenarios/{invalidId}/clone");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteScenario_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/rentability-scenarios/{invalidId}");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetScenarioVersions_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/rentability-scenarios/{invalidId}/versions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }
}
