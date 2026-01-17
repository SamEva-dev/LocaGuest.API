namespace LocaGuest.Application.DTOs.Properties;

public sealed class PropertyListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string Type { get; set; } = string.Empty;
    public string PropertyUsageType { get; set; } = "Complete";
    public string Status { get; set; } = "Vacant";

    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public decimal Surface { get; set; }

    public int? TotalRooms { get; set; }
    public int? OccupiedRooms { get; set; }
    public int? ReservedRooms { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class PropertyDetailReadDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string Type { get; set; } = string.Empty;
    public string PropertyUsageType { get; set; } = "Complete";
    public string Status { get; set; } = "Vacant";

    public decimal Surface { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public bool HasElevator { get; set; }
    public bool HasParking { get; set; }
    public bool HasBalcony { get; set; }
    public bool IsFurnished { get; set; }

    public decimal Rent { get; set; }
    public decimal? Charges { get; set; }
    public decimal? Deposit { get; set; }

    public string? Description { get; set; }

    public int? TotalRooms { get; set; }
    public int? OccupiedRooms { get; set; }
    public int? ReservedRooms { get; set; }

    public int? MinimumStay { get; set; }
    public int? MaximumStay { get; set; }
    public decimal? PricePerNight { get; set; }
    public int? NightsBookedPerMonth { get; set; }

    public List<string> ImageUrls { get; set; } = new();
    public List<PropertyRoomDto> Rooms { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
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
    
    // Informations financières
    public decimal? PropertyTax { get; set; }
    public decimal? CondominiumCharges { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? Insurance { get; set; }
    public decimal? ManagementFeesRate { get; set; }
    public decimal? MaintenanceRate { get; set; }
    public decimal? VacancyRate { get; set; }
    
    // Informations administratives
    public string? CadastralReference { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? TotalWorksAmount { get; set; }
    public string? EnergyClass { get; set; }
    public int? ConstructionYear { get; set; }
}
