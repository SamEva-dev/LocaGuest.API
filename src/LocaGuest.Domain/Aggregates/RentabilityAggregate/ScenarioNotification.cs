using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class ScenarioNotification : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid ScenarioId { get; private set; }
    public string Type { get; private set; } = string.Empty; // shared, commented, updated, restored
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? ActorName { get; private set; }
    
    private ScenarioNotification() { }
    
    public static ScenarioNotification Create(
        Guid userId,
        Guid scenarioId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        string? actorName = null)
    {
        return new ScenarioNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScenarioId = scenarioId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            ActionUrl = actionUrl,
            ActorName = actorName,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }
}
