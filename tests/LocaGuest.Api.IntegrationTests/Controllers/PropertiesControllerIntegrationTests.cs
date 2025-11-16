using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class PropertiesControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LocaGuestWebApplicationFactory _factory;

    public PropertiesControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        // Seed test data after client is created
        Task.Run(async () => await SeedTestDataAsync()).GetAwaiter().GetResult();
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Clear existing data
        context.Properties.RemoveRange(context.Properties);
        await context.SaveChangesAsync();

        // Add test properties
        var property1 = Property.Create(
            "Test Apartment 1",
            "123 Test Street",
            "Paris",
            PropertyType.Apartment,
            1500m,
            2,
            1
        );

        var property2 = Property.Create(
            "Test House 1",
            "456 Test Avenue",
            "Lyon",
            PropertyType.House,
            2500m,
            3,
            2
        );

        context.Properties.AddRange(property1, property2);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetProperties_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/properties?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProperties_ReturnsPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/properties?page=1&pageSize=10");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"items\"");
        content.Should().Contain("\"totalCount\"");
        content.Should().Contain("\"page\"");
        content.Should().Contain("\"pageSize\"");
    }

    [Fact]
    public async Task GetProperties_WithSearch_FiltersResults()
    {
        // Act
        var response = await _client.GetAsync("/api/properties?search=Apartment");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Apartment");
    }

    [Fact]
    public async Task GetProperty_WithValidId_ReturnsProperty()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
        var property = context.Properties.First();

        // Act
        var response = await _client.GetAsync($"/api/properties/{property.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(property.Name);
    }

    [Fact]
    public async Task GetProperty_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/properties/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
