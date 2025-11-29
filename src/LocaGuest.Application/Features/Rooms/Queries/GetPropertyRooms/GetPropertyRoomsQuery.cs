using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Queries.GetPropertyRooms;

/// <summary>
/// Query pour récupérer toutes les chambres d'une propriété
/// </summary>
public record GetPropertyRoomsQuery(Guid PropertyId) : IRequest<Result<List<PropertyRoomDto>>>;
