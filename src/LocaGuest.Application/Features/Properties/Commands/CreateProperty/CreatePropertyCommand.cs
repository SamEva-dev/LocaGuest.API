using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.CreateProperty;

/// <summary>
/// Command for creating a new property
/// </summary>
public record CreatePropertyCommand : IRequest<Result<PropertyDetailDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Surface { get; init; }
    public int? Bedrooms { get; init; }
    public int? Bathrooms { get; init; }
    public int? Floor { get; init; }
    public bool HasElevator { get; init; }
    public bool HasParking { get; init; }
    public bool HasBalcony { get; init; }
    public decimal Rent { get; init; }
    public decimal? Charges { get; init; }
    public string? Description { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public decimal? PurchasePrice { get; init; }
    public string? EnergyClass { get; init; }
    public int? ConstructionYear { get; init; }
    
    // Nouveau: Type d'utilisation
    public string PropertyUsageType { get; init; } = "Complete"; // "Complete", "Colocation", "Airbnb"
    
    // Pour les colocations
    public int? TotalRooms { get; init; }
    
    // Pour Airbnb
    public int? MinimumStay { get; init; }
    public int? MaximumStay { get; init; }
    public decimal? PricePerNight { get; init; }
}
