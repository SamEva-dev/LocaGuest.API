using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;

public record GetInvoicesByTenantQuery(Guid TenantId) : IRequest<Result<List<InvoiceDto>>>;

public record InvoiceDto(
    Guid Id,
    Guid ContractId,
    Guid TenantId,
    Guid PropertyId,
    int Month,
    int Year,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaidDate,
    string Status,
    DateTime GeneratedAt,
    string? Notes
);
