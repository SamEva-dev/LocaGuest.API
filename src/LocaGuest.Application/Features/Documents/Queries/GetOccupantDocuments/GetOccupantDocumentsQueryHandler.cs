using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetOccupantDocuments;

public class GetOccupantDocumentsQueryHandler : IRequestHandler<GetOccupantDocumentsQuery, Result<List<DocumentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupantDocumentsQueryHandler> _logger;

    public GetOccupantDocumentsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupantDocumentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<DocumentDto>>> Handle(GetOccupantDocumentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var occupantId = Guid.Parse(request.OccupantId);
            var documents = await _unitOfWork.Documents.GetByTenantIdAsync(occupantId, cancellationToken);

            // Load occupant name
            var occupant = await _unitOfWork.Occupants.GetByIdAsync(occupantId, cancellationToken);
            
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
                OccupantId = d.AssociatedOccupantId,
                OccupantName = occupant?.FullName,
                PropertyId = d.PropertyId,
                PropertyName = null,
                IsArchived = d.IsArchived,
                CreatedAt = d.CreatedAt
            }).ToList();

            return Result.Success(documentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for occupant {OccupantId}", request.OccupantId);
            return Result.Failure<List<DocumentDto>>($"Error retrieving documents: {ex.Message}");
        }
    }
}
