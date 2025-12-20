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
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public PropertyType Type { get; private set; }
    public PropertyUsageType UsageType { get; private set; }
    public PropertyStatus Status { get; private set; }
    public decimal Rent { get; private set; }
    
    // Pour les colocations
    public int? TotalRooms { get; private set; }
    public int OccupiedRooms { get; private set; } = 0;
    
    /// <summary>
    /// Liste des chambres pour les colocations individuelles
    /// </summary>
    private readonly List<PropertyRoom> _rooms = new();
    public IReadOnlyCollection<PropertyRoom> Rooms => _rooms.AsReadOnly();
    
    /// <summary>
    /// Liste des images/documents associés à la propriété
    /// </summary>
    private readonly List<PropertyImage> _images = new();
    public IReadOnlyCollection<PropertyImage> Images => _images.AsReadOnly();
    
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
    
    // Diagnostics obligatoires
    public string? DpeRating { get; private set; }  // A, B, C, D, E, F, G
    public int? DpeValue { get; private set; }  // kWh/m²/an
    public string? GesRating { get; private set; }  // A, B, C, D, E, F, G
    public DateTime? ElectricDiagnosticDate { get; private set; }
    public DateTime? ElectricDiagnosticExpiry { get; private set; }
    public DateTime? GasDiagnosticDate { get; private set; }
    public DateTime? GasDiagnosticExpiry { get; private set; }
    public bool? HasAsbestos { get; private set; }
    public DateTime? AsbestosDiagnosticDate { get; private set; }
    public string? ErpZone { get; private set; }  // Zone de risques (ERP)
    
    // Informations financières complémentaires
    public decimal? PropertyTax { get; private set; }  // Taxe foncière annuelle
    public decimal? CondominiumCharges { get; private set; }  // Charges de copropriété annuelles

    public decimal? PurchasePrice { get; private set; }

    public decimal? Insurance { get; private set; }
    public decimal? ManagementFeesRate { get; private set; }
    public decimal? MaintenanceRate { get; private set; }
    public decimal? VacancyRate { get; private set; }

    // Airbnb
    public int? NightsBookedPerMonth { get; private set; }
    
    // Informations administratives
    public string? CadastralReference { get; private set; }  // Référence cadastrale
    public string? LotNumber { get; private set; }  // Numéro de lot
    public DateTime? AcquisitionDate { get; private set; }  // Date d'acquisition
    public decimal? TotalWorksAmount { get; private set; }  // Montant total des travaux réalisés
    
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
        if ((usageType == PropertyUsageType.Colocation || usageType == PropertyUsageType.ColocationIndividual || usageType == PropertyUsageType.ColocationSolidaire) 
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
            TotalRooms = (usageType == PropertyUsageType.Colocation || usageType == PropertyUsageType.ColocationIndividual || usageType == PropertyUsageType.ColocationSolidaire) ? totalRooms : null,
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
        string? postalCode = null,
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
        if (postalCode != null) PostalCode = postalCode;
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
    
    public void UpdateDiagnostics(
        string? dpeRating = null,
        int? dpeValue = null,
        string? gesRating = null,
        DateTime? electricDiagnosticDate = null,
        DateTime? electricDiagnosticExpiry = null,
        DateTime? gasDiagnosticDate = null,
        DateTime? gasDiagnosticExpiry = null,
        bool? hasAsbestos = null,
        DateTime? asbestosDiagnosticDate = null,
        string? erpZone = null)
    {
        if (dpeRating != null) DpeRating = dpeRating;
        if (dpeValue.HasValue) DpeValue = dpeValue.Value;
        if (gesRating != null) GesRating = gesRating;
        if (electricDiagnosticDate.HasValue) ElectricDiagnosticDate = electricDiagnosticDate;
        if (electricDiagnosticExpiry.HasValue) ElectricDiagnosticExpiry = electricDiagnosticExpiry;
        if (gasDiagnosticDate.HasValue) GasDiagnosticDate = gasDiagnosticDate;
        if (gasDiagnosticExpiry.HasValue) GasDiagnosticExpiry = gasDiagnosticExpiry;
        if (hasAsbestos.HasValue) HasAsbestos = hasAsbestos;
        if (asbestosDiagnosticDate.HasValue) AsbestosDiagnosticDate = asbestosDiagnosticDate;
        if (erpZone != null) ErpZone = erpZone;
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateFinancialInfo(
        decimal? propertyTax = null,
        decimal? condominiumCharges = null)
    {
        if (propertyTax.HasValue)
        {
            if (propertyTax.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_TAX", "Property tax cannot be negative");
            PropertyTax = propertyTax;
        }

        if (condominiumCharges.HasValue)
        {
            if (condominiumCharges.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_CONDO_CHARGES", "Condominium charges cannot be negative");
            CondominiumCharges = condominiumCharges;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePurchaseInfo(decimal? purchasePrice = null)
    {
        if (purchasePrice.HasValue)
        {
            if (purchasePrice.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_PURCHASE_PRICE", "Purchase price cannot be negative");
            PurchasePrice = purchasePrice.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRentabilityInfo(
        decimal? insurance = null,
        decimal? managementFeesRate = null,
        decimal? maintenanceRate = null,
        decimal? vacancyRate = null,
        int? nightsBookedPerMonth = null)
    {
        if (insurance.HasValue)
        {
            if (insurance.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_INSURANCE", "Insurance cannot be negative");
            Insurance = insurance.Value;
        }

        if (managementFeesRate.HasValue)
        {
            if (managementFeesRate.Value < 0 || managementFeesRate.Value > 100)
                throw new ValidationException("PROPERTY_INVALID_MANAGEMENT_FEES_RATE", "Management fees rate must be between 0 and 100");
            ManagementFeesRate = managementFeesRate.Value;
        }

        if (maintenanceRate.HasValue)
        {
            if (maintenanceRate.Value < 0 || maintenanceRate.Value > 100)
                throw new ValidationException("PROPERTY_INVALID_MAINTENANCE_RATE", "Maintenance rate must be between 0 and 100");
            MaintenanceRate = maintenanceRate.Value;
        }

        if (vacancyRate.HasValue)
        {
            if (vacancyRate.Value < 0 || vacancyRate.Value > 100)
                throw new ValidationException("PROPERTY_INVALID_VACANCY_RATE", "Vacancy rate must be between 0 and 100");
            VacancyRate = vacancyRate.Value;
        }

        if (nightsBookedPerMonth.HasValue)
        {
            if (nightsBookedPerMonth.Value < 0 || nightsBookedPerMonth.Value > 31)
                throw new ValidationException("PROPERTY_INVALID_NIGHTS_BOOKED", "Nights booked per month must be between 0 and 31");
            NightsBookedPerMonth = nightsBookedPerMonth.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateAdministrativeInfo(
        string? cadastralReference = null,
        string? lotNumber = null,
        DateTime? acquisitionDate = null,
        decimal? totalWorksAmount = null)
    {
        if (cadastralReference != null) CadastralReference = cadastralReference;
        if (lotNumber != null) LotNumber = lotNumber;
        if (acquisitionDate.HasValue) AcquisitionDate = acquisitionDate;
        if (totalWorksAmount.HasValue)
        {
            if (totalWorksAmount.Value < 0)
                throw new ValidationException("PROPERTY_INVALID_WORKS_AMOUNT", "Total works amount cannot be negative");
            TotalWorksAmount = totalWorksAmount.Value;
        }
        
        UpdatedAt = DateTime.UtcNow;
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
        
        if (UsageType == PropertyUsageType.Colocation || UsageType == PropertyUsageType.ColocationIndividual || UsageType == PropertyUsageType.ColocationSolidaire)
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
        if (UsageType != PropertyUsageType.ColocationIndividual && UsageType != PropertyUsageType.Colocation)
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
        if (UsageType != PropertyUsageType.ColocationIndividual && UsageType != PropertyUsageType.Colocation)
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
        if (UsageType == PropertyUsageType.ColocationIndividual || UsageType == PropertyUsageType.Colocation)
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
        if (UsageType == PropertyUsageType.ColocationIndividual || UsageType == PropertyUsageType.Colocation)
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
    
    #region Gestion des Chambres (Colocation)
    
    /// <summary>
    /// Ajouter une chambre à une colocation
    /// </summary>
    public PropertyRoom AddRoom(string name, decimal rent, decimal? surface = null, decimal? charges = null, string? description = null)
    {
        if (UsageType != PropertyUsageType.ColocationIndividual && UsageType != PropertyUsageType.Colocation)
            throw new ValidationException("PROPERTY_NOT_COLOCATION", "Only colocation properties can have rooms");
            
        if (_rooms.Count >= (TotalRooms ?? 0))
            throw new ValidationException("PROPERTY_MAX_ROOMS_REACHED", "Maximum number of rooms reached");
            
        var room = PropertyRoom.Create(Id, name, rent, surface, charges, description);
        _rooms.Add(room);
        
        return room;
    }
    
    /// <summary>
    /// Mettre à jour une chambre existante
    /// </summary>
    public void UpdateRoom(Guid roomId, string? name = null, decimal? rent = null, decimal? surface = null, decimal? charges = null, string? description = null)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
            throw new ValidationException("ROOM_NOT_FOUND", "Room not found");
            
        room.UpdateDetails(name, rent, surface, charges, description);
    }
    
    /// <summary>
    /// Supprimer une chambre (uniquement si non occupée)
    /// </summary>
    public void RemoveRoom(Guid roomId)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
            throw new ValidationException("ROOM_NOT_FOUND", "Room not found");
            
        if (room.Status != PropertyRoomStatus.Available)
            throw new ValidationException("ROOM_CANNOT_DELETE_OCCUPIED", "Cannot delete an occupied or reserved room");
            
        _rooms.Remove(room);
    }
    
    /// <summary>
    /// Obtenir toutes les chambres disponibles
    /// </summary>
    public IEnumerable<PropertyRoom> GetAvailableRooms()
    {
        return _rooms.Where(r => r.Status == PropertyRoomStatus.Available);
    }
    
    /// <summary>
    /// Obtenir une chambre par ID
    /// </summary>
    public PropertyRoom? GetRoom(Guid roomId)
    {
        return _rooms.FirstOrDefault(r => r.Id == roomId);
    }
    
    /// <summary>
    /// Réserver une chambre pour un contrat
    /// </summary>
    public void ReserveRoom(Guid roomId, Guid contractId)
    {
        var room = GetRoom(roomId);
        if (room == null)
            throw new ValidationException("ROOM_NOT_FOUND", "Room not found");
            
        room.Reserve(contractId);
        UpdateOccupancyStatusFromRooms();
    }
    
    /// <summary>
    /// Marquer une chambre comme occupée (contrat actif)
    /// </summary>
    public void OccupyRoom(Guid roomId, Guid contractId)
    {
        var room = GetRoom(roomId);
        if (room == null)
            throw new ValidationException("ROOM_NOT_FOUND", "Room not found");
            
        room.Occupy(contractId);
        UpdateOccupancyStatusFromRooms();
    }
    
    /// <summary>
    /// Libérer une chambre (fin de contrat)
    /// </summary>
    public void ReleaseRoom(Guid roomId)
    {
        var room = GetRoom(roomId);
        if (room == null)
            throw new ValidationException("ROOM_NOT_FOUND", "Room not found");
            
        room.Release();
        UpdateOccupancyStatusFromRooms();
    }
    
    /// <summary>
    /// Mettre à jour le statut d'occupation basé sur l'état des chambres
    /// </summary>
    private void UpdateOccupancyStatusFromRooms()
    {
        if (UsageType != PropertyUsageType.ColocationIndividual && UsageType != PropertyUsageType.Colocation)
            return;
            
        var occupiedCount = _rooms.Count(r => r.Status == PropertyRoomStatus.Occupied);
        var reservedCount = _rooms.Count(r => r.Status == PropertyRoomStatus.Reserved);
        
        OccupiedRooms = occupiedCount;
        
        if (occupiedCount == 0 && reservedCount == 0)
        {
            SetStatus(PropertyStatus.Vacant);
        }
        else if (occupiedCount > 0 && occupiedCount < (TotalRooms ?? 0))
        {
            SetStatus(PropertyStatus.PartiallyOccupied);
        }
        else if (occupiedCount >= (TotalRooms ?? 0))
        {
            SetStatus(PropertyStatus.Occupied);
        }
        else if (reservedCount > 0 && occupiedCount == 0)
        {
            SetStatus(PropertyStatus.Reserved);
        }
    }
    
    /// <summary>
    /// Vérifier si une chambre spécifique est disponible
    /// </summary>
    public bool IsRoomAvailable(Guid roomId)
    {
        var room = GetRoom(roomId);
        return room?.IsAvailable() ?? false;
    }
    
    /// <summary>
    /// Mettre à jour la liste des URLs/IDs d'images
    /// </summary>
    public void UpdateImageUrls(List<string> imageUrls)
    {
        ImageUrls = imageUrls ?? new List<string>();
    }
    
    #endregion
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
    Colocation,
    Airbnb                 // Location courte durée type Airbnb
}
