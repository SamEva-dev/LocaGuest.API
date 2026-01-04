using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetTenantDocuments;

public class GetTenantDocumentsQueryHandler : IRequestHandler<GetTenantDocumentsQuery, Result<List<DocumentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantDocumentsQueryHandler> _logger;

    public GetTenantDocumentsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantDocumentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<DocumentDto>>> Handle(GetTenantDocumentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = Guid.Parse(request.TenantId);
            var documents = await _unitOfWork.Documents.GetByTenantIdAsync(tenantId, cancellationToken);

            // Load tenant and property names
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(tenantId, cancellationToken);
            
            var documentDtos = documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                Code = d.Code,
                FileName = d.FileName,
                FilePath = d.FilePath,
                Type = d.Type.ToString(),
                Category = d.Category.ToString(),
                FileSizeBytes = d.FileSizeBytes,
                Description = d.Description,
                ExpiryDate = d.ExpiryDate,
                TenantId = d.AssociatedTenantId,
                TenantName = tenant?.FullName,
                PropertyId = d.PropertyId,
                PropertyName = null, // Could be loaded if needed
                IsArchived = d.IsArchived,
                CreatedAt = d.CreatedAt
            }).ToList();

            return Result.Success(documentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for tenant {TenantId}", request.TenantId);
            return Result.Failure<List<DocumentDto>>($"Error retrieving documents: {ex.Message}");
        }
    }
}
