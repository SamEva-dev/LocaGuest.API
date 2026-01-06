using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PropertyImages.Commands.UploadImages;

public class UploadImagesCommandHandler : IRequestHandler<UploadImagesCommand, Result<UploadImagesResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UploadImagesCommandHandler> _logger;

    public UploadImagesCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        IFileStorageService fileStorage,
        ILogger<UploadImagesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<UploadImagesResponse>> Handle(UploadImagesCommand request, CancellationToken cancellationToken)
    {
        // Vérifier que la propriété existe et appartient au tenant
        var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
        
        if (property == null || !_orgContext.OrganizationId.HasValue || property.OrganizationId != _orgContext.OrganizationId.Value)
        {
            return Result.Failure<UploadImagesResponse>("Propriété non trouvée");
        }

        if (request.Files == null || request.Files.Count == 0)
        {
            return Result.Failure<UploadImagesResponse>("Aucun fichier fourni");
        }

        var uploadedImages = new List<PropertyImageDto>();
      //  var tenantCode = $"T{_orgContext.OrganizationId:D}"; // T001, T002, etc.

        var tenantCode = await _unitOfWork.Organizations.GetTenantNumberAsync(_orgContext.OrganizationId.Value, cancellationToken);

        var propertyName = SanitizeFileName(property.Name);
        
        // Structure: uploads/T001/PropertyName/images/
        var subPath = Path.Combine(tenantCode, propertyName, "images");

        foreach (var file in request.Files)
        {
            // Valider le type de fichier
            if (!IsValidImageType(file.ContentType))
            {
                _logger.LogWarning("Type de fichier invalide: {ContentType}", file.ContentType);
                continue;
            }

            try
            {
                // Générer un nom de fichier unique
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                
                // Sauvegarder le fichier via le service
                var relativePath = await _fileStorage.SaveFileAsync(
                    file.Stream, 
                    fileName, 
                    file.ContentType, 
                    subPath, 
                    cancellationToken);

                // Créer l'entité PropertyImage
                var propertyImage = new PropertyImage(
                    propertyId: property.Id,
                    organizationId: property.OrganizationId,
                    fileName: file.FileName,
                    filePath: relativePath,
                    fileSize: file.Length,
                    mimeType: file.ContentType,
                    category: request.Category
                );

                await _unitOfWork.PropertyImages.AddAsync(propertyImage, cancellationToken);

                uploadedImages.Add(new PropertyImageDto
                {
                    Id = propertyImage.Id,
                    PropertyId = propertyImage.PropertyId,
                    FileName = propertyImage.FileName,
                    FilePath = propertyImage.FilePath,
                    FileSize = propertyImage.FileSize,
                    Category = propertyImage.Category,
                    MimeType = propertyImage.MimeType,
                    CreatedAt = propertyImage.CreatedAt
                });

                _logger.LogInformation("Image uploadée: {FileName} pour propriété {PropertyId}", fileName, property.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload de {FileName}", file.FileName);
            }
        }

        // Mettre à jour la liste des IDs d'images sur la propriété
        var currentImageUrls = property.ImageUrls ?? new List<string>();
        var newImageIds = uploadedImages.Select(img => img.Id.ToString()).ToList();
        currentImageUrls.AddRange(newImageIds);
        property.UpdateImageUrls(currentImageUrls);

        // Sauvegarder dans une seule transaction
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new UploadImagesResponse
        {
            Images = uploadedImages,
            Count = uploadedImages.Count
        });
    }

    private bool IsValidImageType(string contentType)
    {
        var validTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        return validTypes.Contains(contentType.ToLower());
    }

    private string SanitizeFileName(string fileName)
    {
        // Supprimer les caractères invalides pour un nom de dossier
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}
