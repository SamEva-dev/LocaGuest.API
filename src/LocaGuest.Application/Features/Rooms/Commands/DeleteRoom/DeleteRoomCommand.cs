using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Commands.DeleteRoom;

/// <summary>
/// Command pour supprimer une chambre (uniquement si disponible)
/// </summary>
public record DeleteRoomCommand(Guid PropertyId, Guid RoomId) : IRequest<Result>;
