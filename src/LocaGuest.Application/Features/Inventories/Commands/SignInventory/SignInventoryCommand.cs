using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.SignInventory;

public record SignInventoryCommand : IRequest<Result>
{
    public Guid InventoryId { get; init; }
    public string InventoryType { get; init; } = "Entry"; // Entry or Exit
    public string SignerRole { get; init; } = "Tenant"; // Tenant or Agent
    public string SignerName { get; init; } = string.Empty;
    public string SignatureData { get; init; } = string.Empty; // Base64 signature image
}
