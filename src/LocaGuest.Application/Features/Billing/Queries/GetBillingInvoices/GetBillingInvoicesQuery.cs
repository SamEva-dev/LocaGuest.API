using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Queries.GetBillingInvoices;

public record GetBillingInvoicesQuery : IRequest<Result<List<BillingInvoiceDto>>>;

public record BillingInvoiceDto(
    string Id,
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    string Status,
    string? InvoicePdf
);
