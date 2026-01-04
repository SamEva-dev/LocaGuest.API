using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Entities;

/// <summary>
/// Token sécurisé pour invitation d'équipe avec expiration
/// </summary>
public class InvitationToken : AuditableEntity
{
    public Guid TeamMemberId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // Navigation
    public TeamMember? TeamMember { get; set; }

    private InvitationToken() { } // EF Core

    public InvitationToken(
        Guid teamMemberId,
        string email,
        Guid organizationId,
        int expirationHours = 72)
    {
        Id = Guid.NewGuid();
        TeamMemberId = teamMemberId;
        Email = email;
        SetOrganizationId(organizationId);
        Token = GenerateSecureToken();
        ExpiresAt = DateTime.UtcNow.AddHours(expirationHours);
        IsUsed = false;
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return !IsUsed && DateTime.UtcNow < ExpiresAt;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    private static string GenerateSecureToken()
    {
        // Générer un token sécurisé de 32 bytes (256 bits)
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
