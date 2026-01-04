using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Commands.ChangeOccupantStatus;

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

            var tenant = await _unitOfWork.Occupants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure<bool>($"Tenant with ID {request.TenantId} not found");

            // ✅ Règle: on ne change le statut que si le locataire n'est pas associé à un bien
            if (tenant.PropertyId != null)
            {
                return Result.Failure<bool>(
                    "Impossible de changer le statut: le locataire est associé à un bien. Dissociez-le d'abord.");
            }

            switch (request.Status)
            {
                case OccupantStatus.Inactive:
                    tenant.Deactivate();
                    break;

                case OccupantStatus.Active:
                    tenant.SetActive();
                    break;

                case OccupantStatus.Reserved:
                    tenant.SetReserved();
                    break;

                default:
                    return Result.Failure<bool>($"Unsupported tenant status: {request.Status}");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Tenant {TenantId} status changed to {Status}",
                tenant.Id,
                tenant.Status);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error changing tenant status for {TenantId}", request.TenantId);
            return Result.Failure<bool>("Erreur lors du changement de statut du locataire");
        }
    }
}
