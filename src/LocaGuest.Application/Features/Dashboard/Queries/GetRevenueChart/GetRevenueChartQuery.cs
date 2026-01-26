using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;

public record GetRevenueChartQuery(int Month, int Year) : IRequest<Result<RevenueChartDto>>
{
}

public record RevenueChartDto
{
    public List<DailyRevenue> DailyData { get; init; } = new();
}

public record DailyRevenue
{
    public int Day { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal ExpectedRevenue { get; init; }
    public decimal ActualRevenue { get; init; }
    public decimal CollectionRate { get; init; }
}
