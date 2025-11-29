using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Commands.UpdateRoom;

/// <summary>
/// Command pour mettre à jour une chambre
/// </summary>
public record UpdateRoomCommand : IRequest<Result<PropertyRoomDto>>
{
    public Guid PropertyId { get; init; }
    public Guid RoomId { get; init; }
    public string? Name { get; init; }
    public decimal? Rent { get; init; }
    public decimal? Surface { get; init; }
    public decimal? Charges { get; init; }
    public string? Description { get; init; }
}
