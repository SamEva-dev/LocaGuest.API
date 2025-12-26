using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.DeleteContract;

public class DeleteContractCommandHandler : IRequestHandler<DeleteContractCommand, Result<DeleteContractResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteContractCommandHandler> _logger;

    public DeleteContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DeleteContractResultDto>> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<DeleteContractResultDto>("Contract not found");

            // Validation: Seulement Draft peut être supprimé
            if (contract.Status != ContractStatus.Draft)
            {
                return Result.Failure<DeleteContractResultDto>("Only Draft contracts can be deleted");
            }

            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<DeleteContractResultDto>("Property not found");

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

            var payments = await _unitOfWork.Payments.GetByContractIdAsync(contract.Id, cancellationToken);
            foreach (var p in payments)
            {
                _unitOfWork.Payments.Remove(p);
            }

            var documents = await _unitOfWork.Documents.GetByContractIdAsync(contract.Id, cancellationToken);
            var deletedDocuments = 0;
            foreach (var d in documents)
            {
                _unitOfWork.Documents.Remove(d);
                deletedDocuments++;
            }

            var deletedPayments = payments.Count;

            _unitOfWork.Contracts.Remove(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract deleted: {ContractId}", request.ContractId);

            return Result.Success(new DeleteContractResultDto
            {
                DeletedPayments = deletedPayments,
                DeletedDocuments = deletedDocuments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contract {ContractId}", request.ContractId);
            return Result.Failure<DeleteContractResultDto>($"Error deleting contract: {ex.Message}");
        }
    }
}
