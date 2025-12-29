using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Commands.ChangeRoomStatus;

public record ChangeRoomStatusCommand : IRequest<Result<PropertyRoomDto>>
{
    public Guid PropertyId { get; init; }
    public Guid RoomId { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ContractId { get; init; }
    public DateTime? OnHoldUntilUtc { get; init; }
}
