using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetAllDocuments;

public class GetAllDocumentsQueryHandler : IRequestHandler<GetAllDocumentsQuery, Result<List<DocumentDto>>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetAllDocumentsQueryHandler> _logger;

    public GetAllDocumentsQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetAllDocumentsQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<List<DocumentDto>>> Handle(GetAllDocumentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all documents for the authenticated user (filtered automatically by OccupantId via query filter)
            var documents = await _readDb.Documents.AsNoTracking()
                .Where(d => !d.IsArchived)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get all related tenants
            var OccupantIds = documents
                .Where(d => d.AssociatedOccupantId.HasValue)
                .Select(d => d.AssociatedOccupantId!.Value)
                .Distinct()
                .ToList();

            var tenants = await _readDb.Occupants.AsNoTracking()
                .Where(t => OccupantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.FullName, cancellationToken);

            // Get all related properties
            var propertyIds = documents
                .Where(d => d.PropertyId.HasValue)
                .Select(d => d.PropertyId!.Value)
                .Distinct()
                .ToList();

            var properties = await _readDb.Properties.AsNoTracking()
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var documentDtos = documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                Code = d.Code,
                FileName = d.FileName,
                FilePath = d.FilePath,
                Type = d.Type.ToString(),
                Category = d.Category.ToString(),
                FileSizeBytes = d.FileSizeBytes,
                OccupantId = d.AssociatedOccupantId,
                PropertyId = d.PropertyId,
                OccupantName = d.AssociatedOccupantId.HasValue ? tenants.GetValueOrDefault(d.AssociatedOccupantId.Value) : null,
                PropertyName = d.PropertyId.HasValue ? properties.GetValueOrDefault(d.PropertyId.Value) : null,
                Description = d.Description,
                CreatedAt = d.CreatedAt,
                IsArchived = d.IsArchived
            }).ToList();

            _logger.LogInformation("Retrieved {Count} documents for current user", documentDtos.Count);
            return Result.Success(documentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all documents");
            return Result.Failure<List<DocumentDto>>("Error retrieving documents");
        }
    }
}
