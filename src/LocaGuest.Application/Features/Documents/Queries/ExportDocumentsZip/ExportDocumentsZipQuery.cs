using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.ExportDocumentsZip;

public record ExportDocumentsZipQuery : IRequest<byte[]>
{
    public required string TenantId { get; init; }
}
