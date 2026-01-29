using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.UserAggregate;

public class UserSession : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public string SessionToken { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }

    private UserSession() { } // EF Core

    public static UserSession Create(
        string userId,
        string sessionToken,
        string deviceName,
        string browser,
        string ipAddress,
        string location)
    {
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = sessionToken,
            DeviceName = deviceName,
            Browser = browser,
            IpAddress = ipAddress,
            Location = location,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    public void UpdateClientInfo(
        string deviceName,
        string browser,
        string ipAddress,
        string location)
    {
        if (!string.IsNullOrWhiteSpace(deviceName))
            DeviceName = deviceName;
        if (!string.IsNullOrWhiteSpace(browser))
            Browser = browser;
        if (!string.IsNullOrWhiteSpace(ipAddress))
            IpAddress = ipAddress;
        if (!string.IsNullOrWhiteSpace(location))
            Location = location;
    }

    public void Revoke()
    {
        IsActive = false;
    }
}
