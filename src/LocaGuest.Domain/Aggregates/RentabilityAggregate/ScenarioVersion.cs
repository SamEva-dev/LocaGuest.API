using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class ScenarioVersion : AuditableEntity
{
    public Guid ScenarioId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ChangeDescription { get; private set; } = string.Empty;
    public string SnapshotJson { get; private set; } = string.Empty;
    
    private ScenarioVersion() { }
    
    public static ScenarioVersion Create(Guid scenarioId, int versionNumber, string changeDescription, string snapshotJson)
    {
        return new ScenarioVersion
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            VersionNumber = versionNumber,
            ChangeDescription = changeDescription,
            SnapshotJson = snapshotJson,
            CreatedAt = DateTime.UtcNow
        };
    }
}
