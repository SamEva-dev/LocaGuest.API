using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.TenantAggregate;
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

            if (!Guid.TryParse(request.TenantId, out var tenantId))
            {
                return Result.Failure<bool>($"Invalid tenant ID format: {request.TenantId}");
            }

            // Load property
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Property not found: {PropertyId}", propertyId);
                return Result.Failure<bool>("Property not found");
            }

            // Load tenant (renter)
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
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
            _logger.LogError(ex, "Error dissociating tenant {TenantId} from property {PropertyId}",
                request.TenantId, request.PropertyId);
            return Result.Failure<bool>($"Error dissociating tenant: {ex.Message}");
        }
    }
}
