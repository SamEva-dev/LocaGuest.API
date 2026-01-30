using LocaGuest.Domain.Common;
using System.Security.Cryptography;
using System.Text;

namespace LocaGuest.Domain.Entities;

public sealed class TenantOnboardingInvitation : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public Guid? PropertyId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public Guid? OccupantId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    private TenantOnboardingInvitation() { }

    public static TenantOnboardingInvitation Create(
        Guid organizationId,
        string email,
        Guid? propertyId,
        DateTime expiresAtUtc,
        string token)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("organizationId is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("token is required.", nameof(token));

        return new TenantOnboardingInvitation
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            PropertyId = propertyId,
            ExpiresAtUtc = expiresAtUtc,
            TokenHash = ComputeSha256Hex(token),
            CreatedAt = DateTime.UtcNow
        }.WithOrganizationId(organizationId);
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsUsed() => UsedAtUtc.HasValue;

    public void MarkAsUsed(DateTime utcNow, Guid occupantId)
    {
        if (UsedAtUtc.HasValue)
            return;

        UsedAtUtc = utcNow;
        OccupantId = occupantId;
    }

    public bool HasToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return string.Equals(TokenHash, ComputeSha256Hex(token), StringComparison.Ordinal);
    }

    private TenantOnboardingInvitation WithOrganizationId(Guid organizationId)
    {
        SetOrganizationId(organizationId);
        return this;
    }

    private static string ComputeSha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
