using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Queries.ExportInvoices;

public record ExportInvoicesQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? TenantId,
    Guid? PropertyId,
    string Format // "csv" or "excel"
) : IRequest<Result<ExportResultDto>>;

public record ExportResultDto(
    byte[] FileContent,
    string FileName,
    string ContentType
);
