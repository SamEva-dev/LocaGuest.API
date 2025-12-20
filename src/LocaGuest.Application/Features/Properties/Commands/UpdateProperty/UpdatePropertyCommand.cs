using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.UpdateProperty;

/// <summary>
/// Command for updating an existing property
/// </summary>
public record UpdatePropertyCommand : IRequest<Result<PropertyDetailDto>>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Type { get; init; }
    public decimal? Surface { get; init; }
    public int? Bedrooms { get; init; }
    public int? Bathrooms { get; init; }
    public int? Floor { get; init; }
    public bool? HasElevator { get; init; }
    public bool? HasParking { get; init; }
    public bool? IsFurnished { get; init; }
    public decimal? Rent { get; init; }
    public decimal? Charges { get; init; }
    public decimal? Deposit { get; init; }
    public string? Notes { get; init; }
    public List<string>? ImageUrls { get; init; }

    public decimal? PurchasePrice { get; init; }
    public decimal? Insurance { get; init; }
    public decimal? ManagementFeesRate { get; init; }
    public decimal? MaintenanceRate { get; init; }
    public decimal? VacancyRate { get; init; }
    public int? NightsBookedPerMonth { get; init; }
    
    // PropertyUsageType specific
    public string? PropertyUsageType { get; init; }
    public int? TotalRooms { get; init; }
    public int? MinimumStay { get; init; }
    public int? MaximumStay { get; init; }
    public decimal? PricePerNight { get; init; }

    public List<CreatePropertyRoomDto>? Rooms { get; init; }

    // Diagnostics obligatoires
    public string? DpeRating { get; init; }
    public int? DpeValue { get; init; }
    public string? GesRating { get; init; }
    public DateTime? ElectricDiagnosticDate { get; init; }
    public DateTime? ElectricDiagnosticExpiry { get; init; }
    public DateTime? GasDiagnosticDate { get; init; }
    public DateTime? GasDiagnosticExpiry { get; init; }
    public bool? HasAsbestos { get; init; }
    public DateTime? AsbestosDiagnosticDate { get; init; }
    public string? ErpZone { get; init; }
    
    // Informations financières complémentaires
    public decimal? PropertyTax { get; init; }
    public decimal? CondominiumCharges { get; init; }
    
    // Informations administratives
    public string? CadastralReference { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? AcquisitionDate { get; init; }
    public decimal? TotalWorksAmount { get; init; }
}
