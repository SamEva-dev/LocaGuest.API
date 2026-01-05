using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class AuthorizationIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Properties_WithoutPermissions_Returns403()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/properties");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            name = "Prop 1",
            address = "1 rue test",
            city = "Paris",
            type = "Apartment",
            surface = 10,
            rent = 100,
            hasElevator = false,
            hasParking = false,
            hasBalcony = false,
            propertyUsageType = "Complete"
        });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_TeamInvite_WithoutPermissions_Returns403()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/team/invite");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new { email = "x@example.com", role = "Viewer" });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
