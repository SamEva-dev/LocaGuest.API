using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PropertyImages.Commands.DeleteImage;

public class DeleteImageCommandHandler : IRequestHandler<DeleteImageCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<DeleteImageCommandHandler> _logger;

    public DeleteImageCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        IFileStorageService fileStorage,
        ILogger<DeleteImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteImageCommand request, CancellationToken cancellationToken)
    {
        // Récupérer l'image
        var images = await _unitOfWork.PropertyImages.FindAsync(i => i.Id == request.ImageId, cancellationToken);
        var image = images.FirstOrDefault();

        if (image == null)
        {
            return Result.Failure("Image non trouvée");
        }

        // Vérifier que l'image appartient à une propriété du tenant
        var property = await _unitOfWork.Properties.GetByIdAsync(image.PropertyId, cancellationToken);
        if (property == null || !_orgContext.OrganizationId.HasValue || property.OrganizationId != _orgContext.OrganizationId.Value)
        {
            return Result.Failure("Accès non autorisé");
        }

        // Supprimer le fichier physique
        try
        {
            await _fileStorage.DeleteFileAsync(image.FilePath, cancellationToken);
            _logger.LogInformation("Fichier supprimé: {FilePath}", image.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du fichier {FilePath}", image.FilePath);
        }

        // Mettre à jour les imageUrls de la propriété
        var imageUrls = property.ImageUrls ?? new List<string>();
        imageUrls.Remove(image.Id.ToString());
        property.UpdateImageUrls(imageUrls);
        
        // Supprimer l'entité de la DB
        _unitOfWork.PropertyImages.Remove(image);
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Image supprimée: {ImageId}", request.ImageId);
        return Result.Success();
    }
}
