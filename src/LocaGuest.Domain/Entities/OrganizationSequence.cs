namespace LocaGuest.Domain.Entities;

/// <summary>
/// Manages entity numbering sequences per organization
/// Ensures unique auto-incrementing codes for each entity type within an organization
/// </summary>
public class OrganizationSequence
{
    /// <summary>
    /// Organization ID
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Entity prefix (APP, L, M, CTR, PAY, INV...)
    /// </summary>
    public string EntityPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Last generated number for this entity type
    /// </summary>
    public int LastNumber { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
