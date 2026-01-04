using System.Security.Cryptography;
using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Entities;

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2,
    Expired = 3
}

public sealed class Invitation : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;

    // SHA256(secret) stored as bytes, never store the secret in clear.
    public byte[] SecretHash { get; private set; } = Array.Empty<byte>();

    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    private Invitation() { }

    public static Invitation Create(
        Guid organizationId,
        string email,
        string role,
        Guid? createdByUserId,
        TimeSpan ttl,
        out string token)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("organizationId is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("role is required.", nameof(role));
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentException("ttl must be positive.", nameof(ttl));

        var invitationId = Guid.NewGuid();
        var secret = GenerateSecret();

        token = $"{invitationId:D}.{secret}";

        return new Invitation
        {
            Id = invitationId,
            Email = email.Trim(),
            Role = role,
            Status = InvitationStatus.Pending,
            SecretHash = ComputeSha256(secret),
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        }.WithOrganizationId(organizationId);
    }

    public bool IsValidNow(DateTime utcNow)
    {
        if (Status != InvitationStatus.Pending)
            return false;

        return utcNow < ExpiresAtUtc;
    }

    public bool VerifySecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        var hash = ComputeSha256(secret);
        return CryptographicOperations.FixedTimeEquals(hash, SecretHash);
    }

    public void Accept(DateTime utcNow)
    {
        if (Status != InvitationStatus.Pending)
            return;

        if (utcNow >= ExpiresAtUtc)
        {
            Status = InvitationStatus.Expired;
            return;
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        if (Status != InvitationStatus.Pending)
            return;

        Status = InvitationStatus.Revoked;
        RevokedAtUtc = utcNow;
    }

    public void MarkExpired()
    {
        if (Status == InvitationStatus.Pending)
            Status = InvitationStatus.Expired;
    }

    private Invitation WithOrganizationId(Guid organizationId)
    {
        SetOrganizationId(organizationId);
        return this;
    }

    private static string GenerateSecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static byte[] ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        var b64 = Convert.ToBase64String(bytes);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
