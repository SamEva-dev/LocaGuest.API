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
    public string Status { get; set; } = "Vacant";
    
    // Pour les colocations
    public int? TotalRooms { get; set; }
    public int? OccupiedRooms { get; set; }
    
    // Pour Airbnb
    public int? MinimumStay { get; set; }
    public int? MaximumStay { get; set; }
    public decimal? PricePerNight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PropertyDetailDto : PropertyDto
{
    public string? Description { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? EnergyClass { get; set; }
    public int? ConstructionYear { get; set; }
    public List<string> Features { get; set; } = new();
    public int ActiveContractsCount { get; set; }
    public decimal TotalRevenue { get; set; }
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
    
    // Pour les colocations
    public int? TotalRooms { get; set; }
    
    // Pour Airbnb
    public int? MinimumStay { get; set; }
    public int? MaximumStay { get; set; }
    public decimal? PricePerNight { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? EnergyClass { get; set; }
    public int? ConstructionYear { get; set; }
}

public class UpdatePropertyDto : CreatePropertyDto
{
    public Guid Id { get; set; }
}
