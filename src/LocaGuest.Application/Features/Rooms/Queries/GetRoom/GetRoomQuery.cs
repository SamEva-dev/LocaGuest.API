using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Queries.GetRoom;

/// <summary>
/// Query pour récupérer une chambre spécifique
/// </summary>
public record GetRoomQuery(Guid PropertyId, Guid RoomId) : IRequest<Result<PropertyRoomDto>>;
