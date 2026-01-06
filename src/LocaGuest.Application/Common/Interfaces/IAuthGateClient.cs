using System.Net;

namespace LocaGuest.Application.Common.Interfaces;

public interface IAuthGateClient
{
    Task<(HttpStatusCode StatusCode, IReadOnlyList<AuthGateUserDto>? Users)> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<HttpStatusCode> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record AuthGateUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public string? TenantCode { get; init; }
    public bool IsActive { get; init; }
    public bool MfaEnabled { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
