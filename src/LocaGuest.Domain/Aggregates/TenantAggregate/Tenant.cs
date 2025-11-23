using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.TenantAggregate.Events;

namespace LocaGuest.Domain.Aggregates.TenantAggregate;

public class Tenant : AuditableEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., T0001-L0001)
    /// Format: {TenantCode}-L{Number}
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime? MoveInDate { get; private set; }
    public TenantStatus Status { get; private set; }
    public string? Notes { get; private set; }
    
    /// <summary>
    /// Associated Property ID - Tenant can be associated to a property before contract creation
    /// </summary>
    public Guid? PropertyId { get; private set; }
    
    /// <summary>
    /// Associated Property Code (e.g., T0001-APP0001) - for quick reference
    /// </summary>
    public string? PropertyCode { get; private set; }

    private Tenant() { } // EF

    public static Tenant Create(string fullName, string email, string? phone = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Phone = phone,
            Status = TenantStatus.Inactive
        };

        tenant.AddDomainEvent(new TenantCreated(tenant.Id, tenant.FullName, tenant.Email));
        return tenant;
    }

    /// <summary>
    /// Set the auto-generated code (called once after creation)
    /// Code is immutable after being set
    /// </summary>
    public void SetCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
            throw new InvalidOperationException("Code cannot be changed once set");
        
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        
        Code = code;
    }

    public void SetMoveInDate(DateTime moveInDate)
    {
        MoveInDate = moveInDate;
    }

    public void Deactivate()
    {
        if (Status == TenantStatus.Inactive) return;
        Status = TenantStatus.Inactive;
        AddDomainEvent(new TenantDeactivated(Id));
    }

    public void Reactivate()
    {
        if (Status == TenantStatus.Active) return;
        Status = TenantStatus.Active;
    }

    public void UpdateContact(string? email = null, string? phone = null)
    {
        if (email != null) Email = email;
        if (phone != null) Phone = phone;
    }
    
    /// <summary>
    /// Associate this tenant to a property
    /// </summary>
    public void AssociateToProperty(Guid propertyId, string propertyCode)
    {
        PropertyId = propertyId;
        PropertyCode = propertyCode;
    }
    
    /// <summary>
    /// Remove association from property
    /// </summary>
    public void DissociateFromProperty()
    {
        PropertyId = null;
        PropertyCode = null;
    }
}

public enum TenantStatus
{
    Active,
    Inactive
}
