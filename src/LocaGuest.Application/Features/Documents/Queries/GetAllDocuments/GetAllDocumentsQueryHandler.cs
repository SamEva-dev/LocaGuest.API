using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetAllDocuments;

public class GetAllDocumentsQueryHandler : IRequestHandler<GetAllDocumentsQuery, Result<List<DocumentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllDocumentsQueryHandler> _logger;

    public GetAllDocumentsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllDocumentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<DocumentDto>>> Handle(GetAllDocumentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all documents for the authenticated user (filtered automatically by TenantId via query filter)
            var documents = await _unitOfWork.Documents.Query()
                .Where(d => !d.IsArchived)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get all related tenants
            var tenantIds = documents
                .Where(d => d.AssociatedTenantId.HasValue)
                .Select(d => d.AssociatedTenantId!.Value)
                .Distinct()
                .ToList();

            var tenants = await _unitOfWork.Tenants.Query()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.FullName, cancellationToken);

            // Get all related properties
            var propertyIds = documents
                .Where(d => d.PropertyId.HasValue)
                .Select(d => d.PropertyId!.Value)
                .Distinct()
                .ToList();

            var properties = await _unitOfWork.Properties.Query()
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
                TenantId = d.AssociatedTenantId,
                PropertyId = d.PropertyId,
                TenantName = d.AssociatedTenantId.HasValue ? tenants.GetValueOrDefault(d.AssociatedTenantId.Value) : null,
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
