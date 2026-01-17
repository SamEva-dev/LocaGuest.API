using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.DissociateTenant;

public class DissociateTenantCommandHandler : IRequestHandler<DissociateTenantCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DissociateTenantCommandHandler> _logger;

    public DissociateTenantCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DissociateTenantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DissociateTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant context
            if (!_currentUserService.IsAuthenticated)
            {
                return Result.Failure<bool>("User not authenticated");
            }

            if (!Guid.TryParse(request.PropertyId, out var propertyId))
            {
                return Result.Failure<bool>($"Invalid property ID format: {request.PropertyId}");
            }

            if (!Guid.TryParse(request.OccupantId, out var OccupantId))
            {
                return Result.Failure<bool>($"Invalid tenant ID format: {request.OccupantId}");
            }

            // Load property
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Property not found: {PropertyId}", propertyId);
                return Result.Failure<bool>("Property not found");
            }

            // Load tenant (renter)
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(OccupantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {OccupantId}", OccupantId);
                return Result.Failure<bool>("Tenant not found");
            }

            // ⭐ Dissociate tenant from property (bidirectional)
            tenant.DissociateFromProperty();
            property.RemoveTenant(tenant.Code);

            // ⭐ Mark tenant as Inactive (no longer associated to any property)
            tenant.Deactivate();

            _logger.LogInformation(
                "Dissociating tenant {TenantCode} from property {PropertyCode}",
                tenant.Code,
                property.Code);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dissociating tenant {OccupantId} from property {PropertyId}",
                request.OccupantId, request.PropertyId);
            return Result.Failure<bool>($"Error dissociating tenant: {ex.Message}");
        }
    }
}
