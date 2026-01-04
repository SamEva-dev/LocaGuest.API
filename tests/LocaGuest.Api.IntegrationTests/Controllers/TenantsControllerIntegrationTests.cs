using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class TenantsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LocaGuestWebApplicationFactory _factory;

    public TenantsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        Task.Run(async () => await SeedTestDataAsync()).GetAwaiter().GetResult();
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
        
        await context.Database.EnsureCreatedAsync();
        
        context.Occupants.RemoveRange(context.Occupants);
        await context.SaveChangesAsync();

        var tenant1 = Occupant.Create(
            "John Doe",
            "john.doe@test.com",
            "0612345678"
        );

        var tenant2 = Occupant.Create(
            "Jane Smith",
            "jane.smith@test.com",
            "0623456789"
        );

        context.Occupants.AddRange(tenant1, tenant2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTenants_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTenants_ReturnsPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants?page=1&pageSize=10");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"items\"");
        content.Should().Contain("\"totalCount\"");
    }

    [Fact]
    public async Task GetTenants_WithSearch_FiltersResults()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants?search=John");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("John");
    }

    [Fact]
    public async Task GetTenant_WithValidId_ReturnsTenant()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
        var tenant = context.Occupants.First();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{tenant.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(tenant.FullName);
    }

    [Fact]
    public async Task GetTenant_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tenants/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
