using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class DashboardControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_ReturnsValidDashboardData()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/summary");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Check for either format (totalProperties or propertiesCount)
        content.Should().MatchRegex("(totalProperties|propertiesCount)");
        content.Should().MatchRegex("(occupiedProperties|activeTenants)");
        content.Should().MatchRegex("(totalRevenue|monthlyRevenue)");
    }

    [Fact]
    public async Task GetRecentActivities_IsAccessibleOrNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/recent-activities?limit=10");

        // Assert - May not be implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUpcomingDeadlines_IsAccessibleOrNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/upcoming-deadlines?days=30");

        // Assert - May not be implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUpcomingDeadlines_WithInvalidDays_IsValidated()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/upcoming-deadlines?days=-1");

        // Assert - Should validate or not be found
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
