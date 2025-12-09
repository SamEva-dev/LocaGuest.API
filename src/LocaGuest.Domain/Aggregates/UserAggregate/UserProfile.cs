using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.UserAggregate;

/// <summary>
/// Profil utilisateur complet avec informations personnelles et professionnelles
/// </summary>
public class UserProfile : AuditableEntity
{
    public string UserId { get; private set; } = string.Empty; // AuthGate User ID
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public string? Role { get; private set; } // Propriétaire, Gestionnaire, Agent
    public string? Bio { get; private set; }
    public string? PhotoUrl { get; private set; }
    
    private UserProfile() { } // EF

    public static UserProfile Create(
        string userId,
        string firstName,
        string lastName,
        string email)
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };

        return profile;
    }

    public void Update(
        string firstName,
        string lastName,
        string? phone,
        string? company,
        string? role,
        string? bio)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Company = company;
        Role = role;
        Bio = bio;
    }

    public void UpdatePhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
    }
}
