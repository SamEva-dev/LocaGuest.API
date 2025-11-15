namespace LocaGuest.Domain.Common;

public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Identifiant du tenant (organisation/compte) - Requis pour l'isolation multi-tenant
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
