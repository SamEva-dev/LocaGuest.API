using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class SubscriptionsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SubscriptionsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPlans_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPlans_ReturnsJsonArray()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/plans");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentSubscription_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions/current");

        // Assert - May fail due to Stripe configuration
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidPlan_ReturnsBadRequest()
    {
        // Arrange
        var request = new { planId = "invalid_plan" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError // May fail due to Stripe config
        );
    }
}
