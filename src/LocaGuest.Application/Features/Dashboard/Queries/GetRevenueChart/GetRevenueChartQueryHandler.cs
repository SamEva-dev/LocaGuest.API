using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;

public class GetRevenueChartQueryHandler : IRequestHandler<GetRevenueChartQuery, Result<RevenueChartDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetRevenueChartQueryHandler> _logger;

    public GetRevenueChartQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<GetRevenueChartQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<RevenueChartDto>> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated)
                return Result.Failure<RevenueChartDto>("User not authenticated");

            var monthlyData = new List<MonthlyRevenue>();
            var payments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);

            for (int month = 1; month <= 12; month++)
            {
                var firstDayOfMonth = new DateTime(request.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Paiements attendus pour ce mois (ExpectedDate dans ce mois)
                var expectedPayments = payments
                    .Where(p => p.ExpectedDate.Year == request.Year && p.ExpectedDate.Month == month)
                    .ToList();

                var expectedRevenue = expectedPayments.Sum(p => p.AmountDue);

                // Revenus réellement perçus (PaymentDate dans ce mois et status Paid)
                var paidPayments = expectedPayments
                    .Where(p => p.Status == PaymentStatus.Paid && p.PaymentDate.HasValue)
                    .ToList();

                var actualRevenue = paidPayments.Sum(p => p.AmountPaid);

                var collectionRate = expectedRevenue > 0 ? (actualRevenue / expectedRevenue * 100) : 0;

                monthlyData.Add(new MonthlyRevenue
                {
                    Month = month,
                    MonthName = firstDayOfMonth.ToString("MMMM", CultureInfo.GetCultureInfo("fr-FR")),
                    ExpectedRevenue = expectedRevenue,
                    ActualRevenue = actualRevenue,
                    CollectionRate = Math.Round(collectionRate, 1)
                });
            }

            _logger.LogInformation("Retrieved revenue chart for year {Year} for tenant {TenantId}", request.Year, _tenantContext.TenantId);

            return Result.Success(new RevenueChartDto
            {
                MonthlyData = monthlyData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue chart for year {Year} and tenant {TenantId}", request.Year, _tenantContext.TenantId);
            return Result.Failure<RevenueChartDto>($"Error retrieving revenue chart: {ex.Message}");
        }
    }
}
