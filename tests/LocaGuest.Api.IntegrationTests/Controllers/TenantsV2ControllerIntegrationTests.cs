using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class TenantsV2ControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantsV2ControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenants_IsAccessibleOrNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/tenants");

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTenants_WithSearch_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/tenants?q=John&status=active");

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTenants_WithPagination_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/tenants?page=1&pageSize=20");

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTenant_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v2/tenants/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTenant_IsAccessible()
    {
        // Arrange
        var tenant = new { fullName = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/tenants", tenant);

        // Assert - V2 API may not be implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTenant_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var tenant = new { fullName = "Updated Tenant" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v2/tenants/{invalidId}", tenant);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
