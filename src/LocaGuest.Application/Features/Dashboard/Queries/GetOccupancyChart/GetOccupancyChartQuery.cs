using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;

public record GetOccupancyChartQuery(int Year) : IRequest<Result<OccupancyChartDto>>
{
}

public record OccupancyChartDto
{
    public List<MonthlyOccupancy> MonthlyData { get; init; } = new();
}

public record MonthlyOccupancy
{
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public int OccupiedUnits { get; init; }
    public int TotalUnits { get; init; }
    public decimal OccupancyRate { get; init; }
}
