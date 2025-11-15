using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.SubscriptionAggregate;

public class UsageEvent : AuditableEntity
{
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; }
    public string Dimension { get; private set; } = string.Empty; // scenarios_created, exports, ai_suggestions, api_calls, etc.
    public int Value { get; private set; }
    public string? Metadata { get; private set; } // JSON pour contexte additionnel
    public DateTime Timestamp { get; private set; }
    
    private UsageEvent() { }
    
    public static UsageEvent Create(
        Guid subscriptionId,
        Guid userId,
        string dimension,
        int value = 1,
        string? metadata = null)
    {
        return new UsageEvent
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            UserId = userId,
            Dimension = dimension,
            Value = value,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
