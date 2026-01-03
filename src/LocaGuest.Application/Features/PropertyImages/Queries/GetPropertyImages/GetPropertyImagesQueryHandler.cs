using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PropertyImages.Queries.GetPropertyImages;

public class GetPropertyImagesQueryHandler : IRequestHandler<GetPropertyImagesQuery, Result<List<PropertyImageDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<GetPropertyImagesQueryHandler> _logger;

    public GetPropertyImagesQueryHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<GetPropertyImagesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result<List<PropertyImageDto>>> Handle(GetPropertyImagesQuery request, CancellationToken cancellationToken)
    {
        // Vérifier que la propriété existe et appartient au tenant
        var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null || !_orgContext.OrganizationId.HasValue || property.OrganizationId != _orgContext.OrganizationId.Value)
        {
            return Result.Failure<List<PropertyImageDto>>("Propriété non trouvée");
        }

        // Récupérer toutes les images de la propriété
        var images = await _unitOfWork.PropertyImages.FindAsync(
            i => i.PropertyId == request.PropertyId, 
            cancellationToken);

        var imageDtos = images.Select(img => new PropertyImageDto
        {
            Id = img.Id,
            PropertyId = img.PropertyId,
            FileName = img.FileName,
            FilePath = img.FilePath,
            FileSize = img.FileSize,
            Category = img.Category,
            MimeType = img.MimeType,
            CreatedAt = img.CreatedAt
        }).ToList();

        _logger.LogInformation("Retrieved {Count} images for property {PropertyId}", imageDtos.Count, request.PropertyId);
        return Result.Success(imageDtos);
    }
}
