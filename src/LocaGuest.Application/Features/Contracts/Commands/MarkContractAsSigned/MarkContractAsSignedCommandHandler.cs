using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.MarkContractAsSigned;

public class MarkContractAsSignedCommandHandler : IRequestHandler<MarkContractAsSignedCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkContractAsSignedCommandHandler> _logger;

    public MarkContractAsSignedCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkContractAsSignedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkContractAsSignedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            // ✅ Charger la propriété avec ses rooms pour mettre à jour les compteurs colocation
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            // Marquer comme signé
            contract.MarkAsSigned(request.SignedDate ?? DateTime.UtcNow);
            //contract.Activate();

            // ✅ Pour colocation: réserver la/les chambres (Signed => Reserved)
            if (property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation)
            {
                if (!contract.RoomId.HasValue)
                {
                    return Result.Failure("Pour une colocation individuelle, RoomId est obligatoire.");
                }

                property.ReserveRoom(contract.RoomId.Value, contract.Id);
            }
            else if (property.UsageType == PropertyUsageType.ColocationSolidaire)
            {
                // Colocation solidaire: 1 contrat => toutes les chambres réservées
                property.ReserveAllRooms(contract.Id);
            }

            // TODO: Update property, tenant status and cancel other drafts via domain events

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract marked as signed: {ContractId}", request.ContractId);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error marking contract as signed {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking contract as signed {ContractId}", request.ContractId);
            return Result.Failure($"Error marking contract as signed: {ex.Message}");
        }
    }
}
