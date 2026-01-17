using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;

public record SaveGeneratedDocumentCommand : IRequest<Result<DocumentDto>>
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required string Type { get; init; }
    public required string Category { get; init; }
    public required long FileSizeBytes { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? ContractId { get; init; }
    public Guid? OccupantId { get; init; }
    public Guid? PropertyId { get; init; }
    public string? Description { get; init; }
}
