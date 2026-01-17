using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.GetOccupantDocuments;

public record GetOccupantDocumentsQuery : IRequest<Result<List<DocumentDto>>>
{
    public required string OccupantId { get; init; }
}
