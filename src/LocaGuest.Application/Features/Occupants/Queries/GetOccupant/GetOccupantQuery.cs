using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Occupants;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupant;

public record GetOccupantQuery : IRequest<Result<OccupantDetailDto>>
{
    public required string Id { get; init; }
}
