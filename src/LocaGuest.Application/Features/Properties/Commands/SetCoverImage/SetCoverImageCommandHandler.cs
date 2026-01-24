using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.SetCoverImage;

public class SetCoverImageCommandHandler : IRequestHandler<SetCoverImageCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<SetCoverImageCommandHandler> _logger;

    public SetCoverImageCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<SetCoverImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result> Handle(SetCoverImageCommand request, CancellationToken cancellationToken)
    {
        if (!_orgContext.OrganizationId.HasValue)
        {
            return Result.Failure("Accès non autorisé");
        }

        var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property == null || property.OrganizationId != _orgContext.OrganizationId.Value)
        {
            return Result.Failure("Propriété non trouvée");
        }

        var images = await _unitOfWork.PropertyImages.FindAsync(i => i.Id == request.ImageId, cancellationToken);
        var image = images.FirstOrDefault();
        if (image == null || image.PropertyId != property.Id)
        {
            return Result.Failure("Image non trouvée");
        }

        var imageUrls = property.ImageUrls ?? new List<string>();
        var imageIdString = request.ImageId.ToString();

        imageUrls.Remove(imageIdString);
        imageUrls.Insert(0, imageIdString);
        property.UpdateImageUrls(imageUrls);

        _unitOfWork.Properties.Update(property);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Cover image set for property {PropertyId}: {ImageId}", request.PropertyId, request.ImageId);
        return Result.Success();
    }
}
