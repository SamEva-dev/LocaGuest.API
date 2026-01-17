using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Occupants;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetAssociatedOccupants;

public record GetAssociatedOccupantsQuery : IRequest<Result<List<OccupantDto>>>
{
    public required string PropertyId { get; init; }
}
