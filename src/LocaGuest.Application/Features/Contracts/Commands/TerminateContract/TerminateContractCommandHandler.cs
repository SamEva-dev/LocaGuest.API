using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LocaGuest.Application.Features.Contracts.Commands.TerminateContract;

public class TerminateContractCommandHandler : IRequestHandler<TerminateContractCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TerminateContractCommandHandler> _logger;

    public TerminateContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<TerminateContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(TerminateContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            // Terminer le contrat
            contract.Terminate(request.TerminationDate, request.Reason);

            
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(contract.RenterTenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure("Tenant not found");

            // Mettre à jour le bien / chambres et le locataire
            // ========== BIEN / CHAMBRES ==========
            if ((property.UsageType == PropertyUsageType.ColocationIndividual || property.UsageType == PropertyUsageType.Colocation)
                && contract.RoomId.HasValue)
            {
                // Colocation individuelle: libérer la chambre
              
                property.ReleaseRoom(contract.RoomId.Value);
            }
            else
            {
                // Location complète / colocation solidaire
                var hasOtherActiveContractsForProperty = (await _unitOfWork.Contracts
                        .GetByPropertyIdAsync(property.Id, cancellationToken))
                    .Any(c => c.Id != contract.Id && c.Status == ContractStatus.Active);

                if (hasOtherActiveContractsForProperty)
                {
                    property.SetStatus(PropertyStatus.Active);
                }
                else
                {
                    var hasOtherSignedContractsForProperty = (await _unitOfWork.Contracts
                            .GetByPropertyIdAsync(property.Id, cancellationToken))
                        .Any(c => c.Id != contract.Id && c.Status == ContractStatus.Signed);

                    property.SetStatus(hasOtherSignedContractsForProperty ? PropertyStatus.Reserved : PropertyStatus.Vacant);
                }
            }

            // ========== LOCATAIRE ==========
            var contractsForTenant = await _unitOfWork.Contracts.GetByTenantIdAsync(tenant.Id, cancellationToken);
            var hasOtherActiveContractsForTenant = contractsForTenant.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Active);
            if (hasOtherActiveContractsForTenant)
            {
                // Le locataire reste actif via un autre contrat
                tenant.SetActive();
            }
            else
            {
                var hasOtherSignedContractsForTenant = contractsForTenant.Any(c => c.Id != contract.Id && c.Status == ContractStatus.Signed);
                if (hasOtherSignedContractsForTenant)
                {
                    // Si le contrat terminé était le seul actif, le locataire peut être encore Active ici.
                    // SetReserved() n'accepte pas un tenant Active.
                    if (tenant.Status == TenantStatus.Active)
                    {
                        tenant.Deactivate();
                    }
                    tenant.SetReserved();
                }
                else
                {
                    // Aucun autre contrat actif/signé -> dissocier et désactiver
                    tenant.DissociateFromProperty();
                    tenant.Deactivate();
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract terminated: {ContractId} on {Date}", 
                request.ContractId, request.TerminationDate);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error terminating contract {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating contract {ContractId}", request.ContractId);
            return Result.Failure($"Error terminating contract: {ex.Message}");
        }
    }
}
