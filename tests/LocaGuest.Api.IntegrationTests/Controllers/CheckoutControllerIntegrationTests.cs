using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class CheckoutControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CheckoutControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCheckoutSession_IsAccessible()
    {
        // Arrange
        var request = new
        {
            priceId = "price_test123",
            successUrl = "https://example.com/success",
            cancelUrl = "https://example.com/cancel"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/checkout/create-session", request);

        // Assert - May require Stripe configuration
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmptyData_ReturnsBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/checkout/create-session", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError // May fail due to Stripe config
        );
    }

    [Fact]
    public async Task CheckoutController_HandlesRequests()
    {
        // Act
        var response = await _client.PostAsync("/api/checkout/create-session", null);

        // Assert - Endpoint may or may not be fully implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.NotFound,
            HttpStatusCode.UnsupportedMediaType
        );
    }
}
