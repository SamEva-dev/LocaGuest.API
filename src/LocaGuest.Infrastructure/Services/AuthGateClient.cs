using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LocaGuest.Application.Common.Interfaces;

namespace LocaGuest.Infrastructure.Services;

public sealed class AuthGateClient : IAuthGateClient
{
    private readonly HttpClient _httpClient;

    private sealed record AuthGatePagedResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public AuthGateClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(HttpStatusCode StatusCode, IReadOnlyList<AuthGateUserDto>? Users)> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/users?page=1&pageSize=1000", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var payload = JsonSerializer.Deserialize<AuthGatePagedResult<AuthGateUserDto>>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true
                });

            return (response.StatusCode, payload?.Items ?? new List<AuthGateUserDto>());
        }
        catch (JsonException ex)
        {
            var snippet = json.Length > 500 ? json[..500] : json;
            throw new InvalidOperationException(
                $"AuthGate /api/users returned an unexpected JSON payload. Expected a paged object with 'items'. Payload starts with: {snippet}",
                ex);
        }
    }

    public async Task<HttpStatusCode> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"/api/users/{userId}", cancellationToken);
        return response.StatusCode;
    }
}
