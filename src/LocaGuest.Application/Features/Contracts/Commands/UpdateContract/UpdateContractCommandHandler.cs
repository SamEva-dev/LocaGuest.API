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

            // Vérifier que la propriété et le locataire existent
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            if (property == null)
            {
                return Result.Failure($"Property with ID {request.PropertyId} not found");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure($"Tenant with ID {request.TenantId} not found");
            }

            // Valider les dates
            if (request.EndDate <= request.StartDate)
            {
                return Result.Failure("End date must be after start date");
            }

            // Mettre à jour le contrat
            contract.UpdateBasicInfo(
                request.TenantId,
                request.PropertyId,
                request.RoomId,
                request.Type,
                request.StartDate,
                request.EndDate,
                request.Rent,
                request.Charges ?? 0,
                request.Deposit ?? 0
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
}
