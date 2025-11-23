using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Queries.GetOccupancyTrend;

public record GetOccupancyTrendQuery : IRequest<Result<List<OccupancyDataPointDto>>>
{
    public int Days { get; init; } = 30;
}
