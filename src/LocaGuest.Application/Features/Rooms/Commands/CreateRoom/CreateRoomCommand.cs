using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Commands.CreateRoom;

/// <summary>
/// Command pour créer une chambre dans une propriété
/// </summary>
public record CreateRoomCommand : IRequest<Result<PropertyRoomDto>>
{
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Rent { get; init; }
    public decimal? Surface { get; init; }
    public decimal Charges { get; init; }
    public string? Description { get; init; }
}
