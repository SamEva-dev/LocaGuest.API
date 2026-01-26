using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>
{
    public int? Month { get; init; }
    public int? Year { get; init; }
}

public class DashboardSummaryDto
{
    public int PropertiesCount { get; set; }
    public int OccupiedPropertiesCount { get; set; }
    public int ActiveTenants { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PaidTenantsCount { get; set; }
    public decimal CollectionRate { get; set; }
    public int LatePaymentsCount { get; set; }
    public int LateTenantsCount { get; set; }
    public decimal LateTenantsRate { get; set; }
    public decimal OverdueCount { get; set; }
}
