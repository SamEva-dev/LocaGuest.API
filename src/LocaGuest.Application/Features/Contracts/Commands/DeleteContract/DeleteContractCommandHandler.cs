using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.DeleteContract;

public class DeleteContractCommandHandler : IRequestHandler<DeleteContractCommand, Result>
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

    public async Task<Result> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            // Validation: Seulement Draft ou Cancelled peuvent être supprimés
            if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.Cancelled)
            {
                return Result.Failure("Only Draft or Cancelled contracts can be deleted");
            }

            _unitOfWork.Contracts.Remove(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract deleted: {ContractId}", request.ContractId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contract {ContractId}", request.ContractId);
            return Result.Failure($"Error deleting contract: {ex.Message}");
        }
    }
}
