namespace LocaGuest.Domain.Common;

public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Tenant SaaS (organisation/compte) - requis pour l'isolation multi-tenant.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Définit l'identifiant d'organisation. Cette valeur est immuable une fois définie.
    /// </summary>
    public void SetOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

        if (OrganizationId != Guid.Empty && OrganizationId != organizationId)
            throw new InvalidOperationException("OrganizationId is immutable once set.");

        OrganizationId = organizationId;
    }
    
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
