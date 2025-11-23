using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.GetTenantDocuments;

public record GetTenantDocumentsQuery : IRequest<Result<List<DocumentDto>>>
{
    public required string TenantId { get; init; }
}
