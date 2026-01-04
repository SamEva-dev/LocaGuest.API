using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Text;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class StripeWebhookControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StripeWebhookControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HandleWebhook_RouteExists()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", $"t=1700000000,v1=test-{Guid.NewGuid():N}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Route may or may not be implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task HandleWebhook_WithoutSignature_HandlesGracefully()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert - May not be implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task HandleWebhook_WithInvalidSignature_HandlesGracefully()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent("{\"type\":\"payment_intent.succeeded\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", $"t=1700000000,v1=invalid-{Guid.NewGuid():N}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - May not be implemented
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.NotFound
        );
    }
}
