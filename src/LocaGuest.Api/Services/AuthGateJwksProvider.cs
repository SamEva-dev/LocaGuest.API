using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace LocaGuest.Api.Services;

public sealed class AuthGateJwksProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _jwksUrl;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private long _expiresAtUtcTicks = DateTime.MinValue.Ticks;
    private volatile IReadOnlyList<SecurityKey> _cachedKeys = Array.Empty<SecurityKey>();

    public AuthGateJwksProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;

        var authGateUrl = configuration["AuthGate:Url"] ?? "https://localhost:8081";
        _jwksUrl = authGateUrl.TrimEnd('/') + "/.well-known/jwks.json";
    }

    public IEnumerable<SecurityKey> GetSigningKeys(string? kid)
    {
        var keys = _cachedKeys;

        var expiresAtUtc = new DateTime(Interlocked.Read(ref _expiresAtUtcTicks), DateTimeKind.Utc);
        if (keys.Count == 0 || DateTime.UtcNow >= expiresAtUtc)
        {
            RefreshAsync(force: true).GetAwaiter().GetResult();
            keys = _cachedKeys;
        }

        if (string.IsNullOrWhiteSpace(kid))
        {
            return keys;
        }

        var matching = keys.Where(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal)).ToList();
        if (matching.Count > 0)
        {
            return matching;
        }

        RefreshAsync(force: true).GetAwaiter().GetResult();
        keys = _cachedKeys;

        return keys.Where(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
    }

    public Task WarmUpAsync(CancellationToken cancellationToken = default) => RefreshAsync(force: true, cancellationToken);

    private async Task RefreshAsync(bool force, CancellationToken cancellationToken = default)
    {
        var expiresAtUtc = new DateTime(Interlocked.Read(ref _expiresAtUtcTicks), DateTimeKind.Utc);
        if (!force && DateTime.UtcNow < expiresAtUtc && _cachedKeys.Count > 0)
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            expiresAtUtc = new DateTime(Interlocked.Read(ref _expiresAtUtcTicks), DateTimeKind.Utc);
            if (!force && DateTime.UtcNow < expiresAtUtc && _cachedKeys.Count > 0)
            {
                return;
            }

            var httpClient = _httpClientFactory.CreateClient(nameof(AuthGateJwksProvider));

            var jwksJson = await httpClient.GetStringAsync(_jwksUrl, cancellationToken);
            using var jwksDoc = JsonDocument.Parse(jwksJson);
            var keysElement = jwksDoc.RootElement.GetProperty("keys");

            var signingKeys = new List<SecurityKey>();

            foreach (var key in keysElement.EnumerateArray())
            {
                if (!key.TryGetProperty("kty", out var kty) || !string.Equals(kty.GetString(), "RSA", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var n = key.TryGetProperty("n", out var nProp) ? nProp.GetString() : null;
                var e = key.TryGetProperty("e", out var eProp) ? eProp.GetString() : null;
                var kid = key.TryGetProperty("kid", out var kidProp) ? kidProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(n) || string.IsNullOrWhiteSpace(e))
                {
                    continue;
                }

                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64UrlDecode(n),
                    Exponent = Base64UrlDecode(e)
                });

                signingKeys.Add(new RsaSecurityKey(rsa) { KeyId = kid });
            }

            if (signingKeys.Count == 0)
            {
                Log.Warning("JWKS fetched from {JwksUrl} but contained no usable signing keys", _jwksUrl);
                Interlocked.Exchange(ref _expiresAtUtcTicks, DateTime.UtcNow.AddMinutes(1).Ticks);
                _cachedKeys = Array.Empty<SecurityKey>();
                return;
            }

            _cachedKeys = signingKeys;
            Interlocked.Exchange(ref _expiresAtUtcTicks, DateTime.UtcNow.AddMinutes(5).Ticks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh JWKS from {JwksUrl}", _jwksUrl);
            Interlocked.Exchange(ref _expiresAtUtcTicks, DateTime.UtcNow.AddMinutes(1).Ticks);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
