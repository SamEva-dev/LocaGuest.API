using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Occupants;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetAvailableOccupants;

public record GetAvailableOccupantsQuery : IRequest<Result<List<OccupantDto>>>
{
    public required string PropertyId { get; init; }
}
