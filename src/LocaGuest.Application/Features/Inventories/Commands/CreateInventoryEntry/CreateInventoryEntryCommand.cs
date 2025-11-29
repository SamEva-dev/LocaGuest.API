using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.CreateInventoryEntry;

/// <summary>
/// Command pour créer un état des lieux d'entrée
/// </summary>
public record CreateInventoryEntryCommand : IRequest<Result<InventoryEntryDto>>
{
    public Guid PropertyId { get; init; }
    public Guid? RoomId { get; init; }
    public Guid ContractId { get; init; }
    public DateTime InspectionDate { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public bool TenantPresent { get; init; }
    public string? RepresentativeName { get; init; }
    public string? GeneralObservations { get; init; }
    public List<InventoryItemDto> Items { get; init; } = new();
    public List<string> PhotoUrls { get; init; } = new();
}
