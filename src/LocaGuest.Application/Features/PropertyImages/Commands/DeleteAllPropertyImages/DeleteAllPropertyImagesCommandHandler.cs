using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PropertyImages.Commands.DeleteAllPropertyImages;

public class DeleteAllPropertyImagesCommandHandler : IRequestHandler<DeleteAllPropertyImagesCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<DeleteAllPropertyImagesCommandHandler> _logger;

    public DeleteAllPropertyImagesCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IFileStorageService fileStorage,
        ILogger<DeleteAllPropertyImagesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteAllPropertyImagesCommand request, CancellationToken cancellationToken)
    {
        // Vérifier que la propriété existe et appartient au tenant
        var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null || property.TenantId.ToString() != _tenantContext.TenantId.ToString())
        {
            return Result.Failure("Propriété non trouvée");
        }

        // Récupérer toutes les images de la propriété
        var images = await _unitOfWork.PropertyImages.FindAsync(
            i => i.PropertyId == request.PropertyId,
            cancellationToken);

        var imagesList = images.ToList();
        if (imagesList.Count == 0)
        {
            _logger.LogInformation("Aucune image à supprimer pour la propriété {PropertyId}", request.PropertyId);
            return Result.Success();
        }

        // Supprimer tous les fichiers physiques
        foreach (var image in imagesList)
        {
            try
            {
                await _fileStorage.DeleteFileAsync(image.FilePath, cancellationToken);
                _logger.LogInformation("Fichier supprimé: {FilePath}", image.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier {FilePath}", image.FilePath);
            }
        }

        // Supprimer toutes les entités de la DB
        foreach (var image in imagesList)
        {
            _unitOfWork.PropertyImages.Remove(image);
        }

        // Mettre à jour la propriété (vider imageUrls)
        property.UpdateImageUrls(new List<string>());

        // Sauvegarder dans une seule transaction
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Supprimé {Count} images pour la propriété {PropertyId}", imagesList.Count, request.PropertyId);
        return Result.Success();
    }
}
