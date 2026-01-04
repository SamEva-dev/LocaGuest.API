namespace LocaGuest.Infrastructure.Persistence.Entities;

public sealed class IdempotencyRequestEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ClientId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;

    public string ResponseJson { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; }
}
