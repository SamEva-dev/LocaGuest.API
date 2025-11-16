using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class ContractsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContractsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetContractStats_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllContracts_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetContracts_WithPagination_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetContract_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/contracts/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TerminateContract_IsAccessible()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/contracts/{invalidId}/terminate", null);

        // Assert - Endpoint should exist
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
