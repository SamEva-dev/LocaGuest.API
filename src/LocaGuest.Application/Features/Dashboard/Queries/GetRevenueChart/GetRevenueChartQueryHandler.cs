using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;

public class GetRevenueChartQueryHandler : IRequestHandler<GetRevenueChartQuery, Result<RevenueChartDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetRevenueChartQueryHandler> _logger;

    public GetRevenueChartQueryHandler(
        ILocaGuestReadDbContext readDb,
        ICurrentUserService currentUserService,
        ILogger<GetRevenueChartQueryHandler> logger)
    {
        _readDb = readDb;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RevenueChartDto>> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated)
                return Result.Failure<RevenueChartDto>("User not authenticated");

            var startDateUtc = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateUtcExclusive = startDateUtc.AddMonths(1);

            var expectedByDay = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.ExpectedDate >= startDateUtc && p.ExpectedDate < endDateUtcExclusive)
                .GroupBy(p => p.ExpectedDate.Date)
                .Select(g => new { Day = g.Key.Day, Amount = g.Sum(x => x.AmountDue) })
                .ToDictionaryAsync(x => x.Day, x => x.Amount, cancellationToken);

            var actualByDay = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate.HasValue
                            && p.PaymentDate.Value >= startDateUtc
                            && p.PaymentDate.Value < endDateUtcExclusive
                            && (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PaidLate))
                .GroupBy(p => p.PaymentDate!.Value.Date)
                .Select(g => new { Day = g.Key.Day, Amount = g.Sum(x => x.AmountPaid) })
                .ToDictionaryAsync(x => x.Day, x => x.Amount, cancellationToken);

            var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
            var dailyData = new List<DailyRevenue>(capacity: daysInMonth);

            for (var day = 1; day <= daysInMonth; day++)
            {
                expectedByDay.TryGetValue(day, out var expected);
                actualByDay.TryGetValue(day, out var actual);

                var collectionRate = expected > 0 ? (actual / expected * 100m) : 0m;

                dailyData.Add(new DailyRevenue
                {
                    Day = day,
                    Label = $"J{day}",
                    ExpectedRevenue = expected,
                    ActualRevenue = actual,
                    CollectionRate = Math.Round(collectionRate, 1)
                });
            }

            _logger.LogInformation("Retrieved revenue chart for {Month}/{Year}", request.Month, request.Year);

            return Result.Success(new RevenueChartDto { DailyData = dailyData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue chart for {Month}/{Year}", request.Month, request.Year);
            return Result.Failure<RevenueChartDto>("Error retrieving revenue chart");
        }
    }
}
