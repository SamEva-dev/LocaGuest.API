using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Commands.ChangeOccupantStatus;

public class ChangeOccupantStatusCommandHandler : IRequestHandler<ChangeOccupantStatusCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangeOccupantStatusCommandHandler> _logger;

    public ChangeOccupantStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<ChangeOccupantStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ChangeOccupantStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated)
                return Result.Failure<bool>("User not authenticated");

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(request.OccupantId, cancellationToken);
            if (occupant == null)
                return Result.Failure<bool>($"Occupant with ID {request.OccupantId} not found");

            // ✅ Règle: on ne change le statut que si l'occupant n'est pas associé à un bien
            if (occupant.PropertyId != null)
            {
                return Result.Failure<bool>(
                    "Impossible de changer le statut: l'occupant est associé à un bien. Dissociez-le d'abord.");
            }

            switch (request.Status)
            {
                case OccupantStatus.Inactive:
                    occupant.Deactivate();
                    break;

                case OccupantStatus.Active:
                    occupant.SetActive();
                    break;

                case OccupantStatus.Reserved:
                    occupant.SetReserved();
                    break;

                default:
                    return Result.Failure<bool>($"Unsupported occupant status: {request.Status}");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Occupant {OccupantId} status changed to {Status}",
                occupant.Id,
                occupant.Status);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error changing occupant status for {OccupantId}", request.OccupantId);
            return Result.Failure<bool>("Erreur lors du changement de statut de l'occupant");
        }
    }
}
