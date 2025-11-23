using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.GetAllDocuments;

public record GetAllDocumentsQuery : IRequest<Result<List<DocumentDto>>>
{
}
