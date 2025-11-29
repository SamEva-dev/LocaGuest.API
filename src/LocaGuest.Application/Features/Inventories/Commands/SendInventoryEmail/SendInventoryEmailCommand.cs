using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;

public record SendInventoryEmailCommand : IRequest<Result>
{
    public Guid InventoryId { get; init; }
    public string InventoryType { get; init; } = "Entry"; // Entry or Exit
    public string RecipientEmail { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
}
