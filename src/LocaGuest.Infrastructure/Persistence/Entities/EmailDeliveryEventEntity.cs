namespace LocaGuest.Infrastructure.Persistence.Entities;

public sealed class EmailDeliveryEventEntity
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public long TsEvent { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
