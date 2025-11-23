using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>
{
}

public class DashboardSummaryDto
{
    public int PropertiesCount { get; set; }
    public int ActiveTenants { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal MonthlyRevenue { get; set; }
}
