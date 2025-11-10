namespace LocaGuest.Application.DTOs.Rentability;

public record ScenarioVersionDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public int VersionNumber { get; init; }
    public string ChangeDescription { get; init; } = string.Empty;
    public string SnapshotJson { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
