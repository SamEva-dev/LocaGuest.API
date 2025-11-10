using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class ScenarioShare : AuditableEntity
{
    public Guid ScenarioId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid SharedWithUserId { get; private set; }
    public string Permission { get; private set; } = "view"; // view, edit
    public DateTime? ExpiresAt { get; private set; }
    
    private ScenarioShare() { }
    
    public static ScenarioShare Create(Guid scenarioId, Guid ownerId, Guid sharedWithUserId, string permission, DateTime? expiresAt = null)
    {
        return new ScenarioShare
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            OwnerId = ownerId,
            SharedWithUserId = sharedWithUserId,
            Permission = permission,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    
    public bool CanEdit() => Permission == "edit" && !IsExpired();
    
    public bool CanView() => !IsExpired();
}
