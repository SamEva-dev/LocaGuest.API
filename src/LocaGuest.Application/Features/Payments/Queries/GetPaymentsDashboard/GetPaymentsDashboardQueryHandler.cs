using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsDashboard;

public class GetPaymentsDashboardQueryHandler : IRequestHandler<GetPaymentsDashboardQuery, Result<PaymentsDashboardDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPaymentsDashboardQueryHandler> _logger;

    public GetPaymentsDashboardQueryHandler(
        ILocaGuestReadDbContext readDb,
        ICurrentUserService currentUserService,
        ILogger<GetPaymentsDashboardQueryHandler> logger)
    {
        _readDb = readDb;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaymentsDashboardDto>> Handle(GetPaymentsDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Result<PaymentsDashboardDto>.Failure<PaymentsDashboardDto>("User not authenticated");
        }

        try
        {
            var month = request.Month ?? DateTime.UtcNow.Month;
            var year = request.Year ?? DateTime.UtcNow.Year;

            var todayUtcDate = DateTime.UtcNow.Date;

            var periodPaymentsQuery = _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Month == month && p.Year == year);

            var periodPayments = await periodPaymentsQuery
                .Select(p => new
                {
                    p.Id,
                    p.RenterOccupantId,
                    p.PropertyId,
                    p.AmountDue,
                    p.AmountPaid,
                    p.ExpectedDate,
                    p.Status
                })
                .ToListAsync(cancellationToken);

            var totalRevenue = periodPayments.Sum(p => p.AmountPaid);
            var totalExpected = periodPayments.Sum(p => p.AmountDue);

            var overduePayments = periodPayments
                .Where(p => p.Status == PaymentStatus.Late
                            || (p.Status == PaymentStatus.Partial && p.ExpectedDate.Date < todayUtcDate))
                .Select(p => new
                {
                    p.Id,
                    p.RenterOccupantId,
                    p.PropertyId,
                    p.AmountDue,
                    p.AmountPaid,
                    p.ExpectedDate,
                    RemainingAmount = p.AmountDue - p.AmountPaid
                })
                .ToList();

            var totalOverdue = overduePayments.Sum(p => p.RemainingAmount);
            var overdueCount = overduePayments.Count;

            var paidCount = periodPayments.Count(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PaidLate);
            var pendingCount = periodPayments.Count(p => p.Status == PaymentStatus.Pending);

            var collectionRate = totalExpected > 0
                ? Math.Round((totalRevenue / totalExpected) * 100m, 2)
                : 0m;

            var upcomingPaymentsRaw = await _readDb.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending
                            && p.ExpectedDate.Date >= todayUtcDate
                            && p.ExpectedDate.Date <= todayUtcDate.AddDays(7))
                .OrderBy(p => p.ExpectedDate)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.RenterOccupantId,
                    p.PropertyId,
                    p.AmountDue,
                    p.ExpectedDate
                })
                .ToListAsync(cancellationToken);

            var occupantIds = periodPayments.Select(p => p.RenterOccupantId)
                .Concat(upcomingPaymentsRaw.Select(p => p.RenterOccupantId))
                .Concat(overduePayments.Select(p => p.RenterOccupantId))
                .Distinct()
                .ToList();

            var propertyIds = periodPayments.Select(p => p.PropertyId)
                .Concat(upcomingPaymentsRaw.Select(p => p.PropertyId))
                .Concat(overduePayments.Select(p => p.PropertyId))
                .Distinct()
                .ToList();

            var tenantDict = await _readDb.Occupants
                .AsNoTracking()
                .Where(o => occupantIds.Contains(o.Id))
                .Select(o => new { o.Id, o.FullName })
                .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

            var propertyDict = await _readDb.Properties
                .AsNoTracking()
                .Where(p => propertyIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            var upcomingPayments = upcomingPaymentsRaw
                .Select(p => new UpcomingPaymentDto
                {
                    Id = p.Id,
                    OccupantId = p.RenterOccupantId,
                    OccupantName = tenantDict.GetValueOrDefault(p.RenterOccupantId, "Unknown"),
                    PropertyId = p.PropertyId,
                    PropertyName = propertyDict.GetValueOrDefault(p.PropertyId, "Unknown"),
                    AmountDue = p.AmountDue,
                    ExpectedDate = p.ExpectedDate,
                    DaysUntilDue = (int)(p.ExpectedDate.Date - todayUtcDate).TotalDays
                })
                .ToList();

            var topOverduePayments = overduePayments
                .OrderByDescending(p => p.RemainingAmount)
                .ThenBy(p => p.ExpectedDate)
                .Take(5)
                .Select(p => new OverduePaymentSummaryDto
                {
                    Id = p.Id,
                    OccupantId = p.RenterOccupantId,
                    OccupantName = tenantDict.GetValueOrDefault(p.RenterOccupantId, "Unknown"),
                    PropertyId = p.PropertyId,
                    PropertyName = propertyDict.GetValueOrDefault(p.PropertyId, "Unknown"),
                    AmountDue = p.AmountDue,
                    AmountPaid = p.AmountPaid,
                    RemainingAmount = p.RemainingAmount,
                    ExpectedDate = p.ExpectedDate,
                    DaysLate = (int)(todayUtcDate - p.ExpectedDate.Date).TotalDays
                })
                .ToList();

            var dashboard = new PaymentsDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalExpected = totalExpected,
                TotalOverdue = totalOverdue,
                OverdueCount = overdueCount,
                CollectionRate = collectionRate,
                TotalPayments = periodPayments.Count,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                UpcomingPayments = upcomingPayments,
                TopOverduePayments = topOverduePayments
            };

            _logger.LogInformation(
                "Dashboard generated for {Month}/{Year}: Revenue={Revenue}, Overdue={Overdue}", 
                month, year, totalRevenue, totalOverdue);

            return Result<PaymentsDashboardDto>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payments dashboard");
            return Result<PaymentsDashboardDto>.Failure<PaymentsDashboardDto>("Failed to generate dashboard");
        }
    }
}
