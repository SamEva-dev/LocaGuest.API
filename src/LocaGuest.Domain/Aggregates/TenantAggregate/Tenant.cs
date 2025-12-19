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
    
    // 📋 Informations personnelles détaillées
    public DateTime? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public string? Nationality { get; private set; }
    public string? IdNumber { get; private set; }
    
    // 🚨 Contact d'urgence
    public string? EmergencyContact { get; private set; }
    public string? EmergencyPhone { get; private set; }
    
    // 💼 Situation professionnelle
    public string? Occupation { get; private set; }
    public decimal? MonthlyIncome { get; private set; }
    
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
        MoveInDate = EnsureUtc(moveInDate);
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
    /// Update tenant detailed information
    /// </summary>
    public void UpdateDetails(
        DateTime? dateOfBirth = null,
        string? address = null,
        string? city = null,
        string? postalCode = null,
        string? country = null,
        string? nationality = null,
        string? idNumber = null,
        string? emergencyContact = null,
        string? emergencyPhone = null,
        string? occupation = null,
        decimal? monthlyIncome = null,
        string? notes = null)
    {
        if (dateOfBirth.HasValue) DateOfBirth = EnsureUtc(dateOfBirth.Value);
        if (address != null) Address = address;
        if (city != null) City = city;
        if (postalCode != null) PostalCode = postalCode;
        if (country != null) Country = country;
        if (nationality != null) Nationality = nationality;
        if (idNumber != null) IdNumber = idNumber;
        if (emergencyContact != null) EmergencyContact = emergencyContact;
        if (emergencyPhone != null) EmergencyPhone = emergencyPhone;
        if (occupation != null) Occupation = occupation;
        if (monthlyIncome.HasValue) MonthlyIncome = monthlyIncome;
        if (notes != null) Notes = notes;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
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
    
    /// <summary>
    /// Marquer le locataire comme réservé (contrat signé, début futur)
    /// Transition: Inactive → Reserved
    /// </summary>
    public void SetReserved(Guid contractId, DateTime startDate)
    {
        if (Status == TenantStatus.Active)
            throw new InvalidOperationException("Cannot reserve an active tenant");
            
        Status = TenantStatus.Reserved;
        AddDomainEvent(new TenantStatusChanged(Id, TenantStatus.Reserved));
    }
    
    /// <summary>
    /// Activer le locataire (contrat devient actif)
    /// Transition: Reserved → Active
    /// </summary>
    public void SetActive()
    {
        Status = TenantStatus.Active;
        AddDomainEvent(new TenantStatusChanged(Id, TenantStatus.Active));
    }
    
    /// <summary>
    /// Vérifier si le locataire est disponible pour un nouveau contrat
    /// Un locataire Reserved ou Active ne peut pas avoir de nouveau contrat (sauf colocation)
    /// </summary>
    public bool IsAvailableForNewContract()
    {
        return Status == TenantStatus.Inactive;
    }
    
    /// <summary>
    /// Vérifier si le locataire a un contrat actif ou signé
    /// </summary>
    public bool HasActiveOrReservedStatus()
    {
        return Status == TenantStatus.Active || Status == TenantStatus.Reserved;
    }
}

public enum TenantStatus
{
    Active,      // Locataire avec contrat actif (occupant actuel)
    Reserved,    // Locataire avec contrat signé (futur occupant)
    Inactive     // Locataire sans contrat actif
}
