using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Commands.MarkInvoiceAsPaid;

public record MarkInvoiceAsPaidCommand(
    Guid InvoiceId,
    DateTime PaidDate,
    string? Notes = null
) : IRequest<Result<bool>>;
