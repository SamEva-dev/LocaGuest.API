using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
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
            var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Count total properties (all statuses)
            var propertiesCount = await _unitOfWork.Properties.Query()
                .Where(c => c.CreatedAt <= endDate && c.CreatedAt >= startDate)
                .CountAsync(cancellationToken);

            // Count active contracts during the period
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active &&
                           c.StartDate <= endDate &&
                           c.EndDate >= startDate)
                .ToListAsync(cancellationToken);

            var tenantsCount = await _unitOfWork.Occupants.Query()
                .Where(c => c.CreatedAt <= endDate && c.CreatedAt >= startDate)
                .CountAsync(cancellationToken);

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

            // Calculate payment statistics
            // 1. Locataires uniques avec contrats actifs
            var totalActiveTenants = activeContracts
                .Select(c => c.RenterTenantId)
                .Distinct()
                .Count();

            // 2. Paiements pour la période (payés ou payés en retard)
            var periodPayments = await _unitOfWork.Payments.Query()
                .Where(p => p.Month == targetMonth && 
                           p.Year == targetYear &&
                           (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PaidLate))
                .ToListAsync(cancellationToken);

            // 3. Locataires ayant payé (uniques)
            var paidTenantsCount = periodPayments
                .Select(p => p.RenterTenantId)
                .Distinct()
                .Count();

            // 4. Taux de paiement
            var paymentRate = totalActiveTenants > 0 
                ? (decimal)paidTenantsCount / totalActiveTenants 
                : 0m;

            // 5. Paiements en retard pour la période
            var latePayments = await _unitOfWork.Payments.Query()
                .Where(p => p.Month == targetMonth && 
                           p.Year == targetYear &&
                           (p.Status == PaymentStatus.Late || p.Status == PaymentStatus.Partial))
                .ToListAsync(cancellationToken);

            // 6. Locataires avec paiements en retard (uniques)
            var latePaymentsCount = latePayments
                .Select(p => p.RenterTenantId)
                .Distinct()
                .Count();

            // 7. Taux de retard
            var latePaymentRate = totalActiveTenants > 0 
                ? (decimal)latePaymentsCount / totalActiveTenants 
                : 0m;

            var summary = new DashboardSummaryDto
            {
                PropertiesCount = propertiesCount,
                ActiveTenants = tenantsCount,
                OccupancyRate = occupancyRate,
                MonthlyRevenue = monthlyRevenue,
                PaidTenantsCount = paidTenantsCount,
                CollectionRate = paymentRate,
                LatePaymentsCount = latePaymentsCount,
                OverdueCount = latePaymentRate
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
