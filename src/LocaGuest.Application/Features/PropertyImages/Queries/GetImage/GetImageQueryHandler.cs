using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PropertyImages.Queries.GetImage;

public class GetImageQueryHandler : IRequestHandler<GetImageQuery, Result<ImageFileResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<GetImageQueryHandler> _logger;

    public GetImageQueryHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        IFileStorageService fileStorage,
        ILogger<GetImageQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<ImageFileResult>> Handle(GetImageQuery request, CancellationToken cancellationToken)
    {
        // Récupérer l'image depuis la DB
        var images = await _unitOfWork.PropertyImages.FindAsync(i => i.Id == request.ImageId, cancellationToken, asNoTracking: true);
        var image = images.FirstOrDefault();

        if (image == null)
        {
            return Result.Failure<ImageFileResult>("Image non trouvée");
        }

        // Vérifier que l'image appartient à une propriété du tenant
        var property = await _unitOfWork.Properties.GetByIdAsync(image.PropertyId, cancellationToken, asNoTracking: true);
        if (property == null || !_orgContext.OrganizationId.HasValue || property.OrganizationId != _orgContext.OrganizationId.Value)
        {
            return Result.Failure<ImageFileResult>("Accès non autorisé");
        }

        // Lire le fichier
        try
        {
            var fileBytes = await _fileStorage.ReadFileAsync(image.FilePath, cancellationToken);
            
            return Result.Success(new ImageFileResult
            {
                FileBytes = fileBytes,
                ContentType = image.MimeType,
                FileName = image.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture du fichier {FilePath}", image.FilePath);
            return Result.Failure<ImageFileResult>("Erreur lors de la récupération de l'image");
        }
    }
}
