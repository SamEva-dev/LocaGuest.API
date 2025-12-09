using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;

public record GetRevenueChartQuery(int Year) : IRequest<Result<RevenueChartDto>>
{
}

public record RevenueChartDto
{
    public List<MonthlyRevenue> MonthlyData { get; init; } = new();
}

public record MonthlyRevenue
{
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public decimal ExpectedRevenue { get; init; }
    public decimal ActualRevenue { get; init; }
    public decimal CollectionRate { get; init; }
}
