using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
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

            // ✅ Pour colocation: passer la chambre de Reserved -> Occupied (met à jour OccupiedRooms/ReservedRooms)
            if ((property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation) &&
                contract.RoomId.HasValue)
            {
                property.OccupyRoom(contract.RoomId.Value, contract.Id);
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
