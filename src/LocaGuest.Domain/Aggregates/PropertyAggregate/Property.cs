using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.PropertyAggregate.Events;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.PropertyAggregate;

public class Property : AuditableEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., T0001-APP0001)
    /// Format: {TenantCode}-{Prefix}{Number}
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? ZipCode { get; private set; }
    public string? Country { get; private set; }
    public PropertyType Type { get; private set; }
    public PropertyStatus Status { get; private set; }
    public decimal Rent { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public decimal? Surface { get; private set; }
    public bool HasElevator { get; private set; }
    public bool HasParking { get; private set; }
    public int? Floor { get; private set; }
    public bool IsFurnished { get; private set; }
    public decimal? Charges { get; private set; }
    public decimal? Deposit { get; private set; }
    public string? Notes { get; private set; }
    public List<string> ImageUrls { get; private set; } = new();
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// List of associated tenant codes (e.g., ["T0001-L0001", "T0001-L0002"])
    /// Used to track which tenants are associated to this property
    /// </summary>
    public List<string> AssociatedTenantCodes { get; private set; } = new();

    private Property() { } // EF

    public static Property Create(
        string name,
        string address,
        string city,
        PropertyType type,
        decimal rent,
        int bedrooms,
        int bathrooms)
    {
        if (rent < 0)
            throw new ValidationException("PROPERTY_INVALID_RENT", "Rent cannot be negative");

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            City = city,
            Type = type,
            Rent = rent,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            Status = PropertyStatus.Vacant
        };

        property.AddDomainEvent(new PropertyCreated(property.Id, property.Name));
        return property;
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

    public void SetStatus(PropertyStatus newStatus)
    {
        if (Status == newStatus) return;

        var oldStatus = Status;
        Status = newStatus;

        AddDomainEvent(new PropertyStatusChanged(Id, oldStatus, newStatus));
    }

    public void UpdateDetails(
        string? name = null,
        string? address = null,
        decimal? rent = null,
        int? bedrooms = null,
        int? bathrooms = null)
    {
        if (name != null) Name = name;
        if (address != null) Address = address;
        if (rent.HasValue)
        {
            if (rent.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_RENT", "Rent cannot be negative");
            Rent = rent.Value;
        }
        if (bedrooms.HasValue) Bedrooms = bedrooms.Value;
        if (bathrooms.HasValue) Bathrooms = bathrooms.Value;
    }

    public void SetImages(List<string> urls)
    {
        ImageUrls = urls;
    }
    
    /// <summary>
    /// Add a tenant to this property
    /// </summary>
    public void AddTenant(string tenantCode)
    {
        if (!AssociatedTenantCodes.Contains(tenantCode))
        {
            AssociatedTenantCodes.Add(tenantCode);
        }
    }
    
    /// <summary>
    /// Remove a tenant from this property
    /// </summary>
    public void RemoveTenant(string tenantCode)
    {
        AssociatedTenantCodes.Remove(tenantCode);
    }
}

public enum PropertyType
{
    Apartment,
    House,
    Condo,
    Townhouse,
    Duplex,
    Studio
}

public enum PropertyStatus
{
    Vacant,
    Occupied
}
