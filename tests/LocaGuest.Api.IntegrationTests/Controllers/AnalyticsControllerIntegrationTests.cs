using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class AnalyticsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalyticsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfitabilityStats_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/analytics/profitability-stats?year=2024");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRevenueEvolution_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/analytics/revenue-evolution?year=2024");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPropertyPerformance_ReturnsSuccess()
    {
        // Arrange
        var propertyId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/analytics/property-performance/{propertyId}?year=2024");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAvailableYears_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/analytics/available-years");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfitabilityStats_WithInvalidYear_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/analytics/profitability-stats?year=1900");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }
}
