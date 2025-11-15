using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.SubscriptionAggregate;

public class UsageAggregate : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Dimension { get; private set; } = string.Empty;
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public int TotalValue { get; private set; }
    public DateTime LastUpdated { get; private set; }
    
    private UsageAggregate() { }
    
    public static UsageAggregate Create(
        Guid userId,
        Guid subscriptionId,
        string dimension,
        int year,
        int month,
        int initialValue = 0)
    {
        return new UsageAggregate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = subscriptionId,
            Dimension = dimension,
            PeriodYear = year,
            PeriodMonth = month,
            TotalValue = initialValue,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void Increment(int value = 1)
    {
        TotalValue += value;
        LastUpdated = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Reset()
    {
        TotalValue = 0;
        LastUpdated = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }
}
