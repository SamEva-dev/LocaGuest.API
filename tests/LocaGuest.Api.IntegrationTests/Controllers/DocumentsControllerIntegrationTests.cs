using FluentAssertions;
using LocaGuest.Api.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LocaGuest.Api.IntegrationTests.Controllers;

public class DocumentsControllerIntegrationTests : IClassFixture<LocaGuestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentsControllerIntegrationTests(LocaGuestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDocumentStats_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/documents/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTemplates_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/documents/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecentDocuments_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/documents/recent?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecentDocuments_WithInvalidLimit_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/api/documents/recent?limit=-1");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateDocument_IsAccessible()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/documents/generate", request);

        // Assert - May generate empty document or return BadRequest
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
