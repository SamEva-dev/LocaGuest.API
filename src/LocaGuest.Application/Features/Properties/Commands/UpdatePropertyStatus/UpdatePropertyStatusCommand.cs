using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.UpdatePropertyStatus;

/// <summary>
/// Command for updating property status (Vacant, Occupied, PartiallyOccupied, etc.)
/// </summary>
public record UpdatePropertyStatusCommand : IRequest<Result<bool>>
{
    public Guid PropertyId { get; init; }
    public string Status { get; init; } = string.Empty;
}
