using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Queries.GetFinancialStats;

public record GetFinancialStatsQuery(
    int? Year = null,
    int? Month = null
) : IRequest<Result<FinancialStatsDto>>;

public record FinancialStatsDto(
    decimal TotalRevenue,
    decimal TotalPaid,
    decimal TotalPending,
    decimal TotalOverdue,
    int InvoiceCount,
    int PaidInvoiceCount,
    int PendingInvoiceCount,
    int OverdueInvoiceCount,
    List<MonthlyRevenueDto> MonthlyBreakdown
);

public record MonthlyRevenueDto(
    int Month,
    int Year,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    int InvoiceCount
);
