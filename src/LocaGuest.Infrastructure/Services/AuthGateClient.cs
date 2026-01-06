using System.Net;
using System.Net.Http.Json;
using LocaGuest.Application.Common.Interfaces;

namespace LocaGuest.Infrastructure.Services;

public sealed class AuthGateClient : IAuthGateClient
{
    private readonly HttpClient _httpClient;

    public AuthGateClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(HttpStatusCode StatusCode, IReadOnlyList<AuthGateUserDto>? Users)> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/users", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        var users = await response.Content.ReadFromJsonAsync<List<AuthGateUserDto>>(cancellationToken: cancellationToken);
        return (response.StatusCode, users ?? new List<AuthGateUserDto>());
    }

    public async Task<HttpStatusCode> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"/api/users/{userId}", cancellationToken);
        return response.StatusCode;
    }
}
