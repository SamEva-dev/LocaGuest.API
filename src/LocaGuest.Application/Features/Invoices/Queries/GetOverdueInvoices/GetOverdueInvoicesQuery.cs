using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Queries.GetOverdueInvoices;

public record GetOverdueInvoicesQuery : IRequest<Result<List<InvoiceDto>>>;
