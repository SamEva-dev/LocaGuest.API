using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.TenantAggregate.Events;

namespace LocaGuest.Domain.Aggregates.TenantAggregate;

public class Tenant : AuditableEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime? MoveInDate { get; private set; }
    public TenantStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Tenant() { } // EF

    public static Tenant Create(string fullName, string email, string? phone = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Phone = phone,
            Status = TenantStatus.Active
        };

        tenant.AddDomainEvent(new TenantCreated(tenant.Id, tenant.FullName, tenant.Email));
        return tenant;
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
}

public enum TenantStatus
{
    Active,
    Inactive
}
