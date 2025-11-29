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

            // TODO: Update property and tenant status via domain events

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
