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
            // Count total properties (all statuses)
            var propertiesCount = await _unitOfWork.Properties.Query()
                .CountAsync(cancellationToken);

            // Count all tenants (active or not)
            var tenantsCount = await _unitOfWork.Tenants.Query()
                .CountAsync(cancellationToken);

            // Active contracts used for revenue and occupancy-related metrics
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync(cancellationToken);

            // Calculate monthly revenue (sum of rent from active contracts)
            var monthlyRevenue = activeContracts.Sum(c => c.Rent);

            // Calculate occupancy rate
            // Total properties that can be rented (Vacant or Occupied)
            var totalProperties = propertiesCount;

            var occupiedProperties = await _unitOfWork.Properties.Query()
                .Where(p => p.Status == PropertyStatus.Occupied)
                .CountAsync(cancellationToken);

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
