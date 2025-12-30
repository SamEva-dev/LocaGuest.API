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
    public decimal? PurchasePrice { get; init; }
    
    // Nouveau: Type d'utilisation
    public string PropertyUsageType { get; init; } = "Complete"; // "Complete", "Colocation", "Airbnb"
    
    // Pour les colocations
    public int? TotalRooms { get; init; }
    public List<CreatePropertyRoomDto>? Rooms { get; init; }
    
    // Pour Airbnb
    public int? MinimumStay { get; init; }
    public int? MaximumStay { get; init; }
    public decimal? PricePerNight { get; init; }
    
    // Diagnostics obligatoires
    public string? DpeRating { get; init; }  // A, B, C, D, E, F, G
    public int? DpeValue { get; init; }  // kWh/m²/an
    public string? GesRating { get; init; }  // A, B, C, D, E, F, G
    public DateTime? ElectricDiagnosticDate { get; init; }
    public DateTime? ElectricDiagnosticExpiry { get; init; }
    public DateTime? GasDiagnosticDate { get; init; }
    public DateTime? GasDiagnosticExpiry { get; init; }
    public bool? HasAsbestos { get; init; }
    public DateTime? AsbestosDiagnosticDate { get; init; }
    public string? ErpZone { get; init; }  // Zone de risques (ERP)
    
    // Informations financières complémentaires
    public decimal? PropertyTax { get; init; }  // Taxe foncière annuelle
    public decimal? CondominiumCharges { get; init; }  // Charges de copropriété annuelles

    public decimal? Insurance { get; init; }
    public decimal? ManagementFeesRate { get; init; }
    public decimal? MaintenanceRate { get; init; }
    public decimal? VacancyRate { get; init; }

    public int? NightsBookedPerMonth { get; init; }
    
    // Informations administratives
    public string? CadastralReference { get; init; }  // Référence cadastrale
    public string? LotNumber { get; init; }  // Numéro de lot
    public DateTime? PurchaseDate { get; init; }  // Date d'acquisition
    public decimal? TotalWorksAmount { get; init; }  // Montant total des travaux réalisés
    
    // Autres informations
    public bool? IsFurnished { get; init; }
    public decimal? Deposit { get; init; }
    public string? Description { get; init; }

    public string? EnergyClass { get; init; }
    public int? ConstructionYear { get; init; }
}
