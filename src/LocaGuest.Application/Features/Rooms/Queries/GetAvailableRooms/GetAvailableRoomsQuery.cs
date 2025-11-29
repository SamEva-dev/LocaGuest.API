using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Rooms.Queries.GetAvailableRooms;

/// <summary>
/// Query pour récupérer uniquement les chambres disponibles d'une propriété
/// </summary>
public record GetAvailableRoomsQuery(Guid PropertyId) : IRequest<Result<List<PropertyRoomDto>>>;
