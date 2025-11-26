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
    public PropertyUsageType UsageType { get; private set; }
    public PropertyStatus Status { get; private set; }
    public decimal Rent { get; private set; }
    
    // Pour les colocations
    public int? TotalRooms { get; private set; }
    public int OccupiedRooms { get; private set; } = 0;
    
    // Pour Airbnb
    public int? MinimumStay { get; private set; }
    public int? MaximumStay { get; private set; }
    public decimal? PricePerNight { get; private set; }
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
        PropertyUsageType usageType,
        decimal rent,
        int bedrooms,
        int bathrooms,
        int? totalRooms = null)
    {
        if (rent < 0)
            throw new ValidationException("PROPERTY_INVALID_RENT", "Rent cannot be negative");

        // Validation pour colocation
        if ((usageType == PropertyUsageType.ColocationIndividual || usageType == PropertyUsageType.ColocationSolidaire) 
            && (!totalRooms.HasValue || totalRooms.Value <= 0))
            throw new ValidationException("PROPERTY_COLOCATION_ROOMS_REQUIRED", "TotalRooms is required for colocations");

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            City = city,
            Type = type,
            UsageType = usageType,
            Rent = rent,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            Status = PropertyStatus.Vacant,
            TotalRooms = (usageType == PropertyUsageType.ColocationIndividual || usageType == PropertyUsageType.ColocationSolidaire) ? totalRooms : null,
            OccupiedRooms = 0
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
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateExtendedDetails(
        string? city = null,
        string? zipCode = null,
        string? country = null,
        decimal? surface = null,
        int? floor = null,
        bool? hasElevator = null,
        bool? hasParking = null,
        bool? isFurnished = null,
        decimal? charges = null,
        decimal? deposit = null,
        string? notes = null)
    {
        if (city != null) City = city;
        if (zipCode != null) ZipCode = zipCode;
        if (country != null) Country = country;
        if (surface.HasValue)
        {
            if (surface.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_SURFACE", "Surface cannot be negative");
            Surface = surface.Value;
        }
        if (floor.HasValue) Floor = floor.Value;
        if (hasElevator.HasValue) HasElevator = hasElevator.Value;
        if (hasParking.HasValue) HasParking = hasParking.Value;
        if (isFurnished.HasValue) IsFurnished = isFurnished.Value;
        if (charges.HasValue) Charges = charges.Value;
        if (deposit.HasValue) Deposit = deposit.Value;
        if (notes != null) Notes = notes;
        
        UpdatedAt = DateTime.UtcNow;
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
            
            // Mettre à jour le statut en fonction du type de bien
            UpdateOccupancyStatus();
        }
    }
    
    /// <summary>
    /// Remove a tenant from this property
    /// </summary>
    public void RemoveTenant(string tenantCode)
    {
        AssociatedTenantCodes.Remove(tenantCode);
        
        // Mettre à jour le statut en fonction du type de bien
        UpdateOccupancyStatus();
    }
    
    /// <summary>
    /// Met à jour le statut d'occupation en fonction du type de bien et du nombre de locataires
    /// </summary>
    public void UpdateOccupancyStatus()
    {
        OccupiedRooms = AssociatedTenantCodes.Count;
        
        if (UsageType == PropertyUsageType.ColocationIndividual || UsageType == PropertyUsageType.ColocationSolidaire)
        {
            // Pour une colocation
            if (OccupiedRooms == 0)
            {
                SetStatus(PropertyStatus.Vacant);
            }
            else if (OccupiedRooms < TotalRooms)
            {
                SetStatus(PropertyStatus.PartiallyOccupied);
            }
            else
            {
                SetStatus(PropertyStatus.Occupied);
            }
        }
        else
        {
            // Pour une location complète ou Airbnb
            if (OccupiedRooms > 0)
            {
                SetStatus(PropertyStatus.Occupied);
            }
            else
            {
                SetStatus(PropertyStatus.Vacant);
            }
        }
    }
    
    /// <summary>
    /// Incrémenter le nombre de chambres occupées (pour colocation individuelle)
    /// </summary>
    public void IncrementOccupiedRooms()
    {
        if (UsageType != PropertyUsageType.ColocationIndividual)
            throw new ValidationException("PROPERTY_NOT_COLOCATION_INDIVIDUAL", "Only colocation individual properties can increment occupied rooms");
            
        if (OccupiedRooms >= (TotalRooms ?? 0))
            throw new ValidationException("PROPERTY_NO_MORE_ROOMS", "All rooms are already occupied");
            
        OccupiedRooms++;
    }
    
    /// <summary>
    /// Décrémenter le nombre de chambres occupées (pour colocation individuelle)
    /// </summary>
    public void DecrementOccupiedRooms()
    {
        if (UsageType != PropertyUsageType.ColocationIndividual)
            throw new ValidationException("PROPERTY_NOT_COLOCATION_INDIVIDUAL", "Only colocation individual properties can decrement occupied rooms");
            
        if (OccupiedRooms <= 0)
            throw new ValidationException("PROPERTY_NO_OCCUPIED_ROOMS", "No rooms are currently occupied");
            
        OccupiedRooms--;
    }
    
    /// <summary>
    /// Vérifier si le bien est disponible pour un nouveau contrat
    /// </summary>
    public bool IsAvailableForNewContract()
    {
        // Location complète: doit être Vacant ou ne pas avoir de contrat Signed/Active
        if (UsageType == PropertyUsageType.Complete)
        {
            return Status == PropertyStatus.Vacant;
        }
        
        // Colocation solidaire: même règle que location complète
        if (UsageType == PropertyUsageType.ColocationSolidaire)
        {
            return Status == PropertyStatus.Vacant;
        }
        
        // Colocation individuelle: vérifier s'il reste des chambres
        if (UsageType == PropertyUsageType.ColocationIndividual)
        {
            return OccupiedRooms < (TotalRooms ?? 0);
        }
        
        return false;
    }
    
    /// <summary>
    /// Vérifier si une chambre spécifique est disponible (colocation individuelle)
    /// Note: Cette méthode nécessite le contexte des contrats actifs
    /// </summary>
    public bool CanAcceptNewTenant()
    {
        if (UsageType == PropertyUsageType.ColocationIndividual)
        {
            return OccupiedRooms < (TotalRooms ?? 0);
        }
        
        return Status == PropertyStatus.Vacant;
    }
    
    /// <summary>
    /// Marquer le bien comme réservé (contrat signé, début futur)
    /// </summary>
    public void SetReserved(Guid contractId, DateTime startDate)
    {
        if (Status == PropertyStatus.Occupied)
            throw new ValidationException("PROPERTY_ALREADY_OCCUPIED", "Property is already occupied");
        
        var oldStatus = Status;
        Status = PropertyStatus.Reserved;
        AddDomainEvent(new PropertyStatusChanged(Id, oldStatus, PropertyStatus.Reserved));
    }
    
    /// <summary>
    /// Configure les paramètres Airbnb
    /// </summary>
    public void SetAirbnbSettings(int minimumStay, int maximumStay, decimal pricePerNight)
    {
        if (UsageType != PropertyUsageType.Airbnb)
            throw new ValidationException("PROPERTY_NOT_AIRBNB", "This property is not configured for Airbnb");
            
        if (minimumStay <= 0 || maximumStay <= 0 || minimumStay > maximumStay)
            throw new ValidationException("PROPERTY_INVALID_STAY_DURATION", "Invalid stay duration parameters");
            
        if (pricePerNight <= 0)
            throw new ValidationException("PROPERTY_INVALID_PRICE", "Price per night must be positive");
            
        MinimumStay = minimumStay;
        MaximumStay = maximumStay;
        PricePerNight = pricePerNight;
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
    Vacant,              // Libre, disponible à la location
    Reserved,            // Réservé (contrat signé, début futur)
    PartiallyOccupied,   // Pour les colocations partiellement occupées
    Occupied             // Occupé (au moins un contrat actif)
}

public enum PropertyUsageType
{
    Complete,              // Location complète du bien (1 seul contrat)
    ColocationIndividual,  // Colocation avec baux individuels (1 contrat par chambre)
    ColocationSolidaire,   // Colocation avec bail solidaire (1 contrat pour tous)
    Airbnb                 // Location courte durée type Airbnb
}
