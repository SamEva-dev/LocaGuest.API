using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocaGuest.Api.Services;

public interface ITenantOnboardingTokenService
{
    string CreateToken(TenantOnboardingTokenPayload payload, TimeSpan ttl);
    bool TryValidate(string token, out TenantOnboardingTokenPayload payload);
}

public sealed record TenantOnboardingTokenPayload(
    Guid OrganizationId,
    string Email,
    Guid? PropertyId,
    DateTime ExpiresAtUtc);

public sealed class TenantOnboardingTokenService : ITenantOnboardingTokenService
{
    private readonly byte[] _secret;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public TenantOnboardingTokenService(IConfiguration configuration)
    {
        var secret = configuration["TenantOnboarding:TokenSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Missing configuration TenantOnboarding:TokenSecret");

        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public string CreateToken(TenantOnboardingTokenPayload payload, TimeSpan ttl)
    {
        var expiresAtUtc = DateTime.UtcNow.Add(ttl);
        var normalized = payload with { ExpiresAtUtc = expiresAtUtc };

        var payloadJson = JsonSerializer.Serialize(normalized, _jsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadB64 = Base64UrlEncode(payloadBytes);

        var sig = ComputeHmac(payloadB64);
        var sigB64 = Base64UrlEncode(sig);

        return $"{payloadB64}.{sigB64}";
    }

    public bool TryValidate(string token, out TenantOnboardingTokenPayload payload)
    {
        payload = default!;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var payloadB64 = parts[0];
        var sigB64 = parts[1];

        byte[] providedSig;
        try
        {
            providedSig = Base64UrlDecode(sigB64);
        }
        catch
        {
            return false;
        }

        var expectedSig = ComputeHmac(payloadB64);
        if (!CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
            return false;

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(payloadB64);
        }
        catch
        {
            return false;
        }

        TenantOnboardingTokenPayload? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<TenantOnboardingTokenPayload>(payloadBytes, _jsonOptions);
        }
        catch
        {
            return false;
        }

        if (deserialized is null)
            return false;

        if (deserialized.OrganizationId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(deserialized.Email))
            return false;

        if (DateTime.UtcNow >= deserialized.ExpiresAtUtc)
            return false;

        payload = deserialized;
        return true;
    }

    private byte[] ComputeHmac(string payloadB64)
    {
        using var hmac = new HMACSHA256(_secret);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        var b64 = Convert.ToBase64String(bytes);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        return Convert.FromBase64String(s);
    }
}
