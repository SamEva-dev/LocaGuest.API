using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Occupants;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupants;

public record GetOccupantsQuery : IRequest<Result<PagedResult<OccupantDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public string? Status { get; init; }
}
