using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.MarkContractAsExpired;

public class MarkContractAsExpiredCommandHandler : IRequestHandler<MarkContractAsExpiredCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkContractAsExpiredCommandHandler> _logger;

    public MarkContractAsExpiredCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkContractAsExpiredCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkContractAsExpiredCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            // Marquer comme expiré
            contract.MarkAsExpired();

            // TODO: Update property and tenant status via domain events

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Contract marked as expired: {ContractId}", request.ContractId);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error marking contract as expired {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking contract as expired {ContractId}", request.ContractId);
            return Result.Failure($"Error marking contract as expired: {ex.Message}");
        }
    }
}
