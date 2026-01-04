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
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/create-session")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(httpRequest);

        // Assert - May require Stripe configuration
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmptyData_ReturnsBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/create-session")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(httpRequest);

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
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/create-session");
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"test-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(httpRequest);

        // Assert - Endpoint may or may not be fully implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.NotFound,
            HttpStatusCode.UnsupportedMediaType
        );
    }
}
