using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Commands.ReleaseRoom;

/// <summary>
/// Command pour libérer une chambre (retour à Available)
/// </summary>
public record ReleaseRoomCommand(Guid PropertyId, Guid RoomId) : IRequest<Result>;
