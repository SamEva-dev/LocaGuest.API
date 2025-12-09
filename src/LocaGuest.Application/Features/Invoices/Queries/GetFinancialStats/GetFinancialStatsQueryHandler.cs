using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Queries.GetFinancialStats;

public class GetFinancialStatsQueryHandler : IRequestHandler<GetFinancialStatsQuery, Result<FinancialStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetFinancialStatsQueryHandler> _logger;

    public GetFinancialStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetFinancialStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<FinancialStatsDto>> Handle(GetFinancialStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.RentInvoices.Query();

            // Filter by year if specified
            if (request.Year.HasValue)
            {
                query = query.Where(i => i.Year == request.Year.Value);
                
                // Filter by month if both year and month specified
                if (request.Month.HasValue)
                    query = query.Where(i => i.Month == request.Month.Value);
            }
            else
            {
                // Default to current year if no filter
                var currentYear = DateTime.UtcNow.Year;
                query = query.Where(i => i.Year == currentYear);
            }

            var invoices = await query.ToListAsync(cancellationToken);
            var today = DateTime.UtcNow.Date;

            // Calculate totals
            var totalRevenue = invoices.Sum(i => i.Amount);
            var totalPaid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount);
            var totalPending = invoices.Where(i => i.Status == InvoiceStatus.Pending && i.DueDate >= today).Sum(i => i.Amount);
            var totalOverdue = invoices.Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < today).Sum(i => i.Amount);

            var invoiceCount = invoices.Count;
            var paidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid);
            var pendingCount = invoices.Count(i => i.Status == InvoiceStatus.Pending && i.DueDate >= today);
            var overdueCount = invoices.Count(i => i.Status == InvoiceStatus.Pending && i.DueDate < today);

            // Monthly breakdown
            var monthlyBreakdown = invoices
                .GroupBy(i => new { i.Year, i.Month })
                .Select(g => new MonthlyRevenueDto(
                    g.Key.Month,
                    g.Key.Year,
                    g.Sum(i => i.Amount),
                    g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
                    g.Where(i => i.Status == InvoiceStatus.Pending).Sum(i => i.Amount),
                    g.Count()
                ))
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            var result = new FinancialStatsDto(
                totalRevenue,
                totalPaid,
                totalPending,
                totalOverdue,
                invoiceCount,
                paidCount,
                pendingCount,
                overdueCount,
                monthlyBreakdown
            );

            return Result<FinancialStatsDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial stats");
            return Result<FinancialStatsDto>.Failure<FinancialStatsDto>("Erreur lors de la récupération des statistiques financières");
        }
    }
}
