using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
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
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken);
            if (occupant == null)
                return Result.Failure("Tenant not found");

            if (property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation)
            {
                if (!contract.RoomId.HasValue)
                {
                    return Result.Failure("Pour une colocation individuelle, RoomId est obligatoire.");
                }

                // Colocation individuelle: marquer la chambre comme occupée
                property.OccupyRoom(contract.RoomId.Value, contract.Id);
            }
            else if (property.UsageType == PropertyUsageType.ColocationSolidaire)
            {
                // Colocation solidaire: 1 contrat => toutes les chambres occupées
                property.OccupyAllRooms(contract.Id);
            }
            else
            {
                // Location complète / Airbnb / colocation solidaire
                property.SetStatus(PropertyStatus.Active);
            }

            occupant.SetActive();

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
