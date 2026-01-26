using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

    public GetDashboardSummaryQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetDashboardSummaryQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Determine period to filter
            var targetMonth = request.Month ?? DateTime.UtcNow.Month;
            var targetYear = request.Year ?? DateTime.UtcNow.Year;
            var startDateUtc = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateUtcExclusive = startDateUtc.AddMonths(1);

            // Total properties (portfolio size)
            var propertiesCount = await _readDb.Properties
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Contracts overlapping the month (used for occupancy + active tenants)
            var overlappingContractsQuery = _readDb.Contracts
                .AsNoTracking()
                .Where(c => (c.Status == ContractStatus.Active || c.Status == ContractStatus.Signed)
                            && c.StartDate < endDateUtcExclusive
                            && c.EndDate >= startDateUtc);

            var activeTenants = await overlappingContractsQuery
                .Select(c => c.RenterOccupantId)
                .Distinct()
                .CountAsync(cancellationToken);

            var occupiedProperties = await overlappingContractsQuery
                .Select(c => c.PropertyId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Note: occupancyRate is intentionally 0..1 (frontend formats it as %)
            var occupancyRate = propertiesCount > 0
                ? (decimal)occupiedProperties / propertiesCount
                : 0m;

            // Monthly revenue: expected amount due for the selected period
            var monthlyRevenue = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Month == targetMonth && p.Year == targetYear)
                .SumAsync(p => (decimal?)p.AmountDue, cancellationToken) ?? 0m;

            // Calculate payment statistics
            // 1. Locataires uniques avec contrats actifs
            var totalActiveTenants = activeTenants;

            // 2. Paiements pour la période (payés ou payés en retard)
            var paidTenantsCount = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Month == targetMonth
                            && p.Year == targetYear
                            && (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PaidLate))
                .Select(p => p.RenterOccupantId)
                .Distinct()
                .CountAsync(cancellationToken);

            // 4. Taux de paiement
            var paymentRate = totalActiveTenants > 0 
                ? (decimal)paidTenantsCount / totalActiveTenants 
                : 0m;

            // 5. Paiements en retard pour la période
            var latePaymentsCount = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Month == targetMonth
                            && p.Year == targetYear
                            && (p.Status == PaymentStatus.Late || p.Status == PaymentStatus.Partial))
                .CountAsync(cancellationToken);

            // Locataires avec paiements en retard (uniques)
            var lateTenantsCount = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Month == targetMonth
                            && p.Year == targetYear
                            && (p.Status == PaymentStatus.Late || p.Status == PaymentStatus.Partial))
                .Select(p => p.RenterOccupantId)
                .Distinct()
                .CountAsync(cancellationToken);

            // 7. Taux de retard
            var latePaymentRate = totalActiveTenants > 0 
                ? (decimal)lateTenantsCount / totalActiveTenants 
                : 0m;

            var summary = new DashboardSummaryDto
            {
                PropertiesCount = propertiesCount,
                OccupiedPropertiesCount = occupiedProperties,
                ActiveTenants = activeTenants,
                OccupancyRate = occupancyRate,
                MonthlyRevenue = monthlyRevenue,
                PaidTenantsCount = paidTenantsCount,
                CollectionRate = paymentRate,
                LatePaymentsCount = latePaymentsCount,
                LateTenantsCount = lateTenantsCount,
                LateTenantsRate = latePaymentRate,
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
