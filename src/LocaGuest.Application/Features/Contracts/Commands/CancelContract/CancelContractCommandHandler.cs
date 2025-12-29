using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LocaGuest.Application.Features.Contracts.Commands.CancelContract;

public class CancelContractCommandHandler : IRequestHandler<CancelContractCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelContractCommandHandler> _logger;

    public CancelContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CancelContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            if (contract.Status != ContractStatus.Signed)
                return Result.Failure("Only Signed contracts can be cancelled");

            contract.CancelSigned();

            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(contract.RenterTenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure("Tenant not found");

            if (property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation)
            {
                if (!contract.RoomId.HasValue)
                {
                    return Result.Failure("Pour une colocation individuelle, RoomId est obligatoire.");
                }

                property.ReleaseRoom(contract.RoomId.Value);
            }
            else if (property.UsageType == PropertyUsageType.ColocationSolidaire)
            {
                property.ReleaseAllRooms();
            }
            else
            {
                var otherContractsForProperty = await _unitOfWork.Contracts.GetByPropertyIdAsync(property.Id, cancellationToken);
                var hasOtherActive = otherContractsForProperty.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Active);
                if (hasOtherActive)
                {
                    property.SetStatus(PropertyStatus.Active);
                }
                else
                {
                    var hasOtherSigned = otherContractsForProperty.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Signed);
                    property.SetStatus(hasOtherSigned ? PropertyStatus.Reserved : PropertyStatus.Vacant);
                }
            }

            var contractsForTenant = await _unitOfWork.Contracts.GetByTenantIdAsync(tenant.Id, cancellationToken);
            var hasOtherActiveContractsForTenant = contractsForTenant.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Active);
            if (hasOtherActiveContractsForTenant)
            {
                tenant.SetActive();
            }
            else
            {
                var hasOtherSignedContractsForTenant = contractsForTenant.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Signed);
                if (hasOtherSignedContractsForTenant)
                {
                    if (tenant.Status == TenantStatus.Active)
                    {
                        tenant.Deactivate();
                    }
                    tenant.SetReserved();
                }
                else
                {
                    tenant.DissociateFromProperty();
                    tenant.Deactivate();
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract cancelled: {ContractId}", request.ContractId);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling contract {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling contract {ContractId}", request.ContractId);
            return Result.Failure($"Error cancelling contract: {ex.Message}");
        }
    }
}
