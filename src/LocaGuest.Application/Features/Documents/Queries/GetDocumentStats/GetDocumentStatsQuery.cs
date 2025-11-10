using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;

public record GetDocumentStatsQuery : IRequest<Result<DocumentStatsDto>>
{
}
