using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

    public GetDashboardSummaryQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetDashboardSummaryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Determine period to filter
            var targetMonth = request.Month ?? DateTime.UtcNow.Month;
            var targetYear = request.Year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Count total properties (all statuses)
            var propertiesCount = await _unitOfWork.Properties.Query()
                .CountAsync(cancellationToken);

            // Count active contracts during the period
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active &&
                           c.StartDate <= endDate &&
                           (c.EndDate == null || c.EndDate >= startDate))
                .ToListAsync(cancellationToken);

            var tenantsCount = activeContracts.Count;

            // Calculate monthly revenue (sum of rent from active contracts in period)
            var monthlyRevenue = activeContracts.Sum(c => c.Rent);

            // Calculate occupancy rate for the period
            var totalProperties = propertiesCount;
            var occupiedProperties = activeContracts
                .Select(c => c.PropertyId)
                .Distinct()
                .Count();

            var occupancyRate = totalProperties > 0 
                ? (decimal)occupiedProperties / totalProperties 
                : 0m;

            var summary = new DashboardSummaryDto
            {
                PropertiesCount = propertiesCount,
                ActiveTenants = tenantsCount,
                OccupancyRate = occupancyRate,
                MonthlyRevenue = monthlyRevenue
            };

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary");
            return Result.Failure<DashboardSummaryDto>("Error retrieving dashboard summary");
        }
    }
}
