using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.UpdateContract;

public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateContractCommandHandler> _logger;

    public UpdateContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Charger le contrat existant
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
            {
                return Result.Failure($"Contract with ID {request.ContractId} not found");
            }

            // Vérifier que le contrat est un Draft (seulement les Drafts peuvent être modifiés)
            if (contract.Status != ContractStatus.Draft)
            {
                return Result.Failure($"Only Draft contracts can be updated. Current status: {contract.Status}");
            }

            var OccupantId = request.OccupantIdIsSet ? request.OccupantId : contract.RenterOccupantId;
            var propertyId = request.PropertyIdIsSet ? request.PropertyId : contract.PropertyId;
            var roomId = request.RoomIdIsSet ? request.RoomId : contract.RoomId;
            var type = request.TypeIsSet ? request.Type : contract.Type.ToString();
            var startDate = request.StartDateIsSet ? request.StartDate : contract.StartDate;
            var endDate = request.EndDateIsSet ? request.EndDate : contract.EndDate;
            var rent = request.RentIsSet ? request.Rent : contract.Rent;
            var charges = request.ChargesIsSet ? request.Charges : contract.Charges;
            var deposit = request.DepositIsSet ? request.Deposit : contract.Deposit;

            if (OccupantId is null)
                return Result.Failure("OccupantId is required");
            if (propertyId is null)
                return Result.Failure("PropertyId is required");
            if (string.IsNullOrWhiteSpace(type))
                return Result.Failure("Type is required");
            if (startDate is null)
                return Result.Failure("StartDate is required");
            if (endDate is null)
                return Result.Failure("EndDate is required");
            if (rent is null)
                return Result.Failure("Rent is required");

            var normalizedType = NormalizeContractType(type);
            if (normalizedType is null)
                return Result.Failure($"Invalid contract type: {type}");

            // Vérifier que la propriété et le locataire existent
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId.Value, cancellationToken);
            if (property == null)
            {
                return Result.Failure($"Property with ID {propertyId} not found");
            }

            var tenant = await _unitOfWork.Occupants.GetByIdAsync(OccupantId.Value, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure($"Tenant with ID {OccupantId} not found");
            }

            // Valider les dates
            if (endDate.Value <= startDate.Value)
            {
                return Result.Failure("End date must be after start date");
            }

            // Mettre à jour le contrat
            contract.UpdateBasicInfo(
                OccupantId.Value,
                propertyId.Value,
                roomId,
                normalizedType,
                startDate.Value,
                endDate.Value,
                rent.Value,
                charges ?? 0,
                deposit
            );

            _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract {ContractId} updated successfully", request.ContractId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {ContractId}", request.ContractId);
            return Result.Failure($"Error updating contract: {ex.Message}");
        }
    }

    private static string? NormalizeContractType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Trim();

        if (s.Equals("Furnished", StringComparison.OrdinalIgnoreCase))
            return "Furnished";
        if (s.Equals("Unfurnished", StringComparison.OrdinalIgnoreCase))
            return "Unfurnished";

        if (s.Equals("Meublé", StringComparison.OrdinalIgnoreCase) || s.Equals("Meuble", StringComparison.OrdinalIgnoreCase))
            return "Furnished";
        if (s.Equals("Non meublé", StringComparison.OrdinalIgnoreCase) || s.Equals("Non meuble", StringComparison.OrdinalIgnoreCase))
            return "Unfurnished";

        return null;
    }
}
