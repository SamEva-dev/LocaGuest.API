using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

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
            contract.Terminate(request.TerminationDate);

            // TODO: Update property and tenant status via domain events or separate commands

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
