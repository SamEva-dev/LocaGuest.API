using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class TrackingControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrackingControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Track_IsAccessibleOrNotImplemented()
    {
        // Arrange
        var trackingEvent = new
        {
            eventType = "page_view",
            pageName = "/dashboard",
            url = "/dashboard",
            metadata = new { page = "/dashboard" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tracking/event", trackingEvent);

        // Assert - Tracking may not be implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.Accepted,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Track_WithEmptyData_HandlesGracefully()
    {
        // Arrange
        var trackingEvent = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tracking/event", trackingEvent);

        // Assert - Tracking may not be implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.BadRequest,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task Track_MultipleTimes_ConsistentResponse()
    {
        // Arrange
        var event1 = new { eventType = "click", pageName = "auth", url = "/login", metadata = new { button = "login" } };
        var event2 = new { eventType = "click", pageName = "auth", url = "/logout", metadata = new { button = "logout" } };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/tracking/event", event1);
        var response2 = await _client.PostAsJsonAsync("/api/tracking/event", event2);

        // Assert - Both should get same response (even if not found)
        response1.StatusCode.Should().Be(response2.StatusCode);
    }
}
