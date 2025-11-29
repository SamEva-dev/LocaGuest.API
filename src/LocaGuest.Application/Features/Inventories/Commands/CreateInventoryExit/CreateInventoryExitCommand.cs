using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.CreateInventoryExit;

/// <summary>
/// Command pour créer un état des lieux de sortie
/// </summary>
public record CreateInventoryExitCommand : IRequest<Result<InventoryExitDto>>
{
    public Guid PropertyId { get; init; }
    public Guid? RoomId { get; init; }
    public Guid ContractId { get; init; }
    public Guid InventoryEntryId { get; init; }
    public DateTime InspectionDate { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public bool TenantPresent { get; init; }
    public string? RepresentativeName { get; init; }
    public string? GeneralObservations { get; init; }
    public List<InventoryComparisonDto> Comparisons { get; init; } = new();
    public List<DegradationDto> Degradations { get; init; } = new();
    public List<string> PhotoUrls { get; init; } = new();
    public decimal? OwnerCoveredAmount { get; init; }
    public string? FinancialNotes { get; init; }
}
