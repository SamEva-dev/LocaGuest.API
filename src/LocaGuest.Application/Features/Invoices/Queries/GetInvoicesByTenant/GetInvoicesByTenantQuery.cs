using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;

public record GetInvoicesByTenantQuery(Guid OccupantId) : IRequest<Result<List<InvoiceDto>>>;

public record InvoiceDto(
    Guid Id,
    Guid ContractId,
    Guid OccupantId,
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
