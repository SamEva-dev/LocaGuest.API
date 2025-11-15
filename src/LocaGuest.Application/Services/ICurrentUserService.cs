namespace LocaGuest.Application.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
}
