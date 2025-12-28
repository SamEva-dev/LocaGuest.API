using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Commands.GenerateInvoicePdf;

public record GenerateInvoicePdfCommand(Guid InvoiceId) : IRequest<Result<Guid>>;
