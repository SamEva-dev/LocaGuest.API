using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.ActivateContract;

public class ActivateContractCommandHandler : IRequestHandler<ActivateContractCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateContractCommandHandler> _logger;

    public ActivateContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ActivateContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivateContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            // Activer le contrat
            contract.Activate();

            // Mettre à jour le bien (Active / PartialActive) et le locataire (Active)
            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(contract.RenterTenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure("Tenant not found");

            if ((property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation)
                && contract.RoomId.HasValue)
            {
                // Colocation individuelle: marquer la chambre comme occupée
                property.OccupyRoom(contract.RoomId.Value, contract.Id);
            }
            else
            {
                // Location complète / Airbnb / colocation solidaire
                property.SetStatus(PropertyStatus.Active);
            }

            tenant.SetActive();

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract activated: {ContractId}", request.ContractId);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error activating contract {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating contract {ContractId}", request.ContractId);
            return Result.Failure($"Error activating contract: {ex.Message}");
        }
    }
}
