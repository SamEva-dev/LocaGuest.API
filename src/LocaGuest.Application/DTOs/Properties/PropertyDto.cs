namespace LocaGuest.Application.DTOs.Properties;

public class PropertyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;  // ✅ T0001-APP0001
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PropertyUsageType { get; set; } = "Complete";  // Complete, Colocation, Airbnb
    public decimal Surface { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public bool HasElevator { get; set; }
    public bool HasParking { get; set; }
    public bool HasBalcony { get; set; }
    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Vacant";
    
    // Pour les colocations
    public int? TotalRooms { get; set; }
    public int? OccupiedRooms { get; set; }
    public int? ReservedRooms { get; set; }
    public List<PropertyRoomDto> Rooms { get; set; } = new();  // ✅ Liste des chambres
    
    // Pour Airbnb
    public int? MinimumStay { get; set; }
    public int? MaximumStay { get; set; }
    public decimal? PricePerNight { get; set; }
    
    // Diagnostics obligatoires
    public string? DpeRating { get; set; }  // A, B, C, D, E, F, G
    public int? DpeValue { get; set; }  // kWh/m²/an
    public string? GesRating { get; set; }  // A, B, C, D, E, F, G
    public DateTime? ElectricDiagnosticDate { get; set; }
    public DateTime? ElectricDiagnosticExpiry { get; set; }
    public DateTime? GasDiagnosticDate { get; set; }
    public DateTime? GasDiagnosticExpiry { get; set; }
    public bool? HasAsbestos { get; set; }
    public DateTime? AsbestosDiagnosticDate { get; set; }
    public string? ErpZone { get; set; }  // Zone de risques (ERP)
    
    // Informations financières complémentaires
    public decimal? PropertyTax { get; set; }  // Taxe foncière annuelle
    public decimal? CondominiumCharges { get; set; }  // Charges de copropriété annuelles

    // Champs rentabilité (persistés sur l'entité Property)
    public decimal? PurchasePrice { get; set; }
    public decimal? Insurance { get; set; }
    public decimal? ManagementFeesRate { get; set; }
    public decimal? MaintenanceRate { get; set; }
    public decimal? VacancyRate { get; set; }
    public int? NightsBookedPerMonth { get; set; }

    public string? EnergyClass { get; set; }
    public int? ConstructionYear { get; set; }
    
    // Informations administratives
    public string? CadastralReference { get; set; }  // Référence cadastrale
    public string? LotNumber { get; set; }  // Numéro de lot
    public DateTime? PurchaseDate { get; set; }  // Date d'achat / acquisition
    public decimal? TotalWorksAmount { get; set; }  // Montant total des travaux réalisés
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();  // URLs des images
}

public class PropertyDetailDto : PropertyDto
{
    public List<string> Features { get; set; } = new();
    public int ActiveContractsCount { get; set; }
    public decimal TotalRevenue { get; set; }

    public decimal? Deposit { get; set; }
    public bool IsFurnished { get; set; }
    
    /// <summary>
    /// Liste des chambres pour les colocations
    /// </summary>
    public new List<PropertyRoomDto> Rooms { get; set; } = new();
}

/// <summary>
/// DTO pour une chambre de colocation
/// </summary>
public class PropertyRoomDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Surface { get; set; }
    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Available"; // Available, Reserved, Occupied
    public Guid? CurrentContractId { get; set; }
}

/// <summary>
/// DTO pour créer une chambre
/// </summary>
public class CreatePropertyRoomDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? Surface { get; set; }
    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public string? Description { get; set; }
}

public class CreatePropertyDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PropertyUsageType { get; set; } = "Complete";  // Complete, Colocation, Airbnb
    public decimal Surface { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public bool HasElevator { get; set; }
    public bool HasParking { get; set; }
    public bool HasBalcony { get; set; }
    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public string? Description { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? EnergyClass { get; set; }
    public int? ConstructionYear { get; set; }
    
    // Pour les colocations
    public int? TotalRooms { get; set; }
    public List<CreatePropertyRoomDto>? Rooms { get; set; }
    
    // Pour Airbnb
    public int? MinimumStay { get; set; }
    public int? MaximumStay { get; set; }
    public decimal? PricePerNight { get; set; }
    
    // Diagnostics obligatoires
    public string? DpeRating { get; set; }
    public int? DpeValue { get; set; }
    public string? GesRating { get; set; }
    public DateTime? ElectricDiagnosticDate { get; set; }
    public DateTime? ElectricDiagnosticExpiry { get; set; }
    public DateTime? GasDiagnosticDate { get; set; }
    public DateTime? GasDiagnosticExpiry { get; set; }
    public bool? HasAsbestos { get; set; }
    public DateTime? AsbestosDiagnosticDate { get; set; }
    public string? ErpZone { get; set; }
    
    // Informations financières complémentaires
    public decimal? PropertyTax { get; set; }
    public decimal? CondominiumCharges { get; set; }
    
    // Informations administratives
    public string? CadastralReference { get; set; }
    public string? LotNumber { get; set; }
    public decimal? TotalWorksAmount { get; set; }
}

public class UpdatePropertyDto : CreatePropertyDto
{
    public Guid Id { get; set; }
}
