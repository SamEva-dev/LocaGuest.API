using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;

public record GetDocumentTemplatesQuery : IRequest<Result<List<DocumentTemplateDto>>>
{
}
