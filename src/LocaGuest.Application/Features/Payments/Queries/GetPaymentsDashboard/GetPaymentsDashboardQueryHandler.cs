using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsDashboard;

public class GetPaymentsDashboardQueryHandler : IRequestHandler<GetPaymentsDashboardQuery, Result<PaymentsDashboardDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetPaymentsDashboardQueryHandler> _logger;

    public GetPaymentsDashboardQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<GetPaymentsDashboardQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PaymentsDashboardDto>> Handle(GetPaymentsDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsAuthenticated)
        {
            return Result<PaymentsDashboardDto>.Failure<PaymentsDashboardDto>("User not authenticated");
        }

        try
        {
            var month = request.Month ?? DateTime.UtcNow.Month;
            var year = request.Year ?? DateTime.UtcNow.Year;

            // Get all payments
            var allPayments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);
            
            // Filter for the specified month/year
            var periodPayments = allPayments
                .Where(p => p.Month == month && p.Year == year)
                .ToList();

            // Get tenants and properties for names
            var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
            var properties = await _unitOfWork.Properties.GetAllAsync(cancellationToken);

            var tenantDict = tenants.ToDictionary(t => t.Id, t => t.FullName);
            var propertyDict = properties.ToDictionary(p => p.Id, p => p.Name);

            // Calculate KPIs
            var totalRevenue = periodPayments.Sum(p => p.AmountPaid);
            var totalExpected = periodPayments.Sum(p => p.AmountDue);
            
            var overduePayments = periodPayments
                .Where(p => p.Status == PaymentStatus.Late || 
                           (p.Status == PaymentStatus.Partial && p.ExpectedDate < DateTime.UtcNow))
                .ToList();
            
            var totalOverdue = overduePayments.Sum(p => p.GetRemainingAmount());
            var overdueCount = overduePayments.Count;

            var paidCount = periodPayments.Count(p => p.IsPaid());
            var pendingCount = periodPayments.Count(p => p.Status == PaymentStatus.Pending);

            var collectionRate = totalExpected > 0 
                ? Math.Round((totalRevenue / totalExpected) * 100, 2) 
                : 0;

            // Get upcoming payments (next 7 days)
            var upcomingPayments = allPayments
                .Where(p => p.Status == PaymentStatus.Pending && 
                           p.ExpectedDate >= DateTime.UtcNow.Date &&
                           p.ExpectedDate <= DateTime.UtcNow.Date.AddDays(7))
                .OrderBy(p => p.ExpectedDate)
                .Take(10)
                .Select(p => new UpcomingPaymentDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    TenantName = tenantDict.GetValueOrDefault(p.TenantId, "Unknown"),
                    PropertyId = p.PropertyId,
                    PropertyName = propertyDict.GetValueOrDefault(p.PropertyId, "Unknown"),
                    AmountDue = p.AmountDue,
                    ExpectedDate = p.ExpectedDate,
                    DaysUntilDue = (int)(p.ExpectedDate.Date - DateTime.UtcNow.Date).TotalDays
                })
                .ToList();

            // Get top 5 overdue payments
            var topOverduePayments = overduePayments
                .OrderByDescending(p => p.GetRemainingAmount())
                .ThenBy(p => p.ExpectedDate)
                .Take(5)
                .Select(p => new OverduePaymentSummaryDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    TenantName = tenantDict.GetValueOrDefault(p.TenantId, "Unknown"),
                    PropertyId = p.PropertyId,
                    PropertyName = propertyDict.GetValueOrDefault(p.PropertyId, "Unknown"),
                    AmountDue = p.AmountDue,
                    AmountPaid = p.AmountPaid,
                    RemainingAmount = p.GetRemainingAmount(),
                    ExpectedDate = p.ExpectedDate,
                    DaysLate = (int)(DateTime.UtcNow.Date - p.ExpectedDate.Date).TotalDays
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
