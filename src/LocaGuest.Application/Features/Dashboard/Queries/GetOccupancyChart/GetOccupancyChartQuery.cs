using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;

public record GetOccupancyChartQuery(int Month, int Year) : IRequest<Result<OccupancyChartDto>>
{
}

public record OccupancyChartDto
{
    public List<DailyOccupancy> DailyData { get; init; } = new();
}

public record DailyOccupancy
{
    public int Day { get; init; }
    public string Label { get; init; } = string.Empty;
    public int OccupiedUnits { get; init; }
    public int TotalUnits { get; init; }
    public decimal OccupancyRate { get; init; }
}
