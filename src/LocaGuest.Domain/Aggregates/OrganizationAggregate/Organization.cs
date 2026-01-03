using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.OrganizationAggregate.Events;

namespace LocaGuest.Domain.Aggregates.OrganizationAggregate;

/// <summary>
/// Represents a multi-tenant organization (agency, owner, company)
/// Each organization is isolated and has its own users and data
/// </summary>
public class Organization : AuditableEntity
{
    /// <summary>
    /// Auto-incremented global number for organization
    /// </summary>
    public int Number { get; private set; }

    /// <summary>
    /// Human-readable code (T0001, T0002, ...)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Organization name (agency name, company name, owner name)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Organization email
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Organization phone
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Organization status
    /// </summary>
    public OrganizationStatus Status { get; private set; }

    /// <summary>
    /// Subscription plan
    /// </summary>
    public string? SubscriptionPlan { get; private set; }

    /// <summary>
    /// Subscription expiry date
    /// </summary>
    public DateTime? SubscriptionExpiryDate { get; private set; }

    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; private set; }

    // 🎨 Branding Settings
    /// <summary>
    /// Organization logo URL (can be local path or cloud storage URL)
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// Primary brand color (hex format: #FF0000)
    /// </summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>
    /// Secondary brand color (hex format: #0000FF)
    /// </summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>
    /// Accent brand color (hex format: #00FF00)
    /// </summary>
    public string? AccentColor { get; private set; }

    /// <summary>
    /// Company website URL
    /// </summary>
    public string? Website { get; private set; }

    private Organization() { } // EF Core

    public static Organization Create(int number, string name, string email, string? phone = null)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Number = number,
            Code = $"T{number:0000}", // T0001, T0002, etc.
            Name = name,
            Email = email,
            Phone = phone,
            Status = OrganizationStatus.Active,
            SubscriptionPlan = "Trial",
            SubscriptionExpiryDate = DateTime.UtcNow.AddDays(30) // 30-day trial
        };

        organization.SetOrganizationId(organization.Id);

        organization.AddDomainEvent(new OrganizationCreated(
            organization.Id,
            organization.Code,
            organization.Name,
            organization.Email
        ));

        return organization;
    }

    public void UpdateInfo(string? name = null, string? email = null, string? phone = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(email)) Email = email;
        if (phone != null) Phone = phone;
    }

    public void Activate()
    {
        if (Status == OrganizationStatus.Active) return;
        Status = OrganizationStatus.Active;
    }

    public void Suspend()
    {
        if (Status == OrganizationStatus.Suspended) return;
        Status = OrganizationStatus.Suspended;
        AddDomainEvent(new OrganizationSuspended(Id, Code));
    }

    public void Deactivate()
    {
        if (Status == OrganizationStatus.Inactive) return;
        Status = OrganizationStatus.Inactive;
        AddDomainEvent(new OrganizationDeactivated(Id, Code));
    }

    public void UpdateSubscription(string plan, DateTime expiryDate)
    {
        SubscriptionPlan = plan;
        SubscriptionExpiryDate = expiryDate;
    }

    public bool IsSubscriptionActive()
    {
        return SubscriptionExpiryDate.HasValue && SubscriptionExpiryDate.Value > DateTime.UtcNow;
    }

    public void UpdateBrandingSettings(
        string? logoUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? accentColor = null,
        string? website = null)
    {
        if (logoUrl != null) LogoUrl = logoUrl;
        if (primaryColor != null) PrimaryColor = primaryColor;
        if (secondaryColor != null) SecondaryColor = secondaryColor;
        if (accentColor != null) AccentColor = accentColor;
        if (website != null) Website = website;
    }
}

public enum OrganizationStatus
{
    Active,
    Suspended,
    Inactive
}
