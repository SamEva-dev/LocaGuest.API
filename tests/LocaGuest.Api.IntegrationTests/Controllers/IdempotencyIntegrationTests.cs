using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public sealed class IdempotencyIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IdempotencyIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StripeWebhook_SameSignatureAndBody_ReplaysResponse()
    {
        // Arrange
        var signature = $"t=1700000000,v1=dummy-{Guid.NewGuid():N}";
        var body = "{}";

        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request1.Headers.TryAddWithoutValidation("Stripe-Signature", signature);

        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request2.Headers.TryAddWithoutValidation("Stripe-Signature", signature);

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response2.StatusCode.Should().Be(response1.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Be(content1);

        // In testing/dev, middleware/controller may respond with either empty body or ProblemDetails.
        // We only care about replay returning the exact same content.
    }

    [Fact]
    public async Task StripeWebhook_SameSignatureSameBodyDifferentQuery_Returns409Conflict()
    {
        // Arrange
        var signature = $"t=1700000000,v1=dummy-{Guid.NewGuid():N}";
        var body = "{}";

        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request1.Headers.TryAddWithoutValidation("Stripe-Signature", signature);

        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe?x=1")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request2.Headers.TryAddWithoutValidation("Stripe-Signature", signature);

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response2.Content.Headers.ContentType.Should().NotBeNull();
        response2.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var conflictJson = await response2.Content.ReadAsStringAsync();
        conflictJson.Should().Contain("\"status\":409");
        conflictJson.Should().Contain("Idempotency-Key reuse with different payload");
    }
}
