using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Deposits.Commands.RecordDepositReceived;

public class RecordDepositReceivedCommandHandler : IRequestHandler<RecordDepositReceivedCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordDepositReceivedCommandHandler> _logger;

    public RecordDepositReceivedCommandHandler(IUnitOfWork unitOfWork, ILogger<RecordDepositReceivedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RecordDepositReceivedCommand request, CancellationToken cancellationToken)
    {
        Guid depositId = Guid.Empty;
        try
        {
            var deposit = await _unitOfWork.Deposits.GetByContractIdAsync(request.ContractId, cancellationToken);
            if (deposit == null)
                return Result.Failure<Guid>("Deposit not found");

            depositId = deposit.Id;

            deposit.RecordReceive(request.Amount, request.DateUtc, request.Reference);

            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success(deposit.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                try
                {
                    var entityName = entry.Metadata.Name;
                    var key = string.Join(",", entry.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}={p.CurrentValue}"));

                    _logger.LogWarning(
                        "Concurrency conflict on entity {Entity} ({Key}) State={State}",
                        entityName,
                        key,
                        entry.State);

                    // If the conflict concerns an UPDATE/DELETE, refresh the original values from DB.
                    // This resolves cases where a concurrency token (explicit or implicit) changed.
                    if (entry.State is EntityState.Modified or EntityState.Deleted)
                    {
                        var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                        if (databaseValues == null)
                        {
                            return Result.Failure<Guid>($"Entity {entityName} no longer exists.");
                        }

                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
                catch
                {
                    _logger.LogWarning("Concurrency conflict on unknown entity entry");
                }
            }

            _logger.LogWarning(ex, "Concurrency error recording deposit received for contract {ContractId}. Retrying once...", request.ContractId);

            try
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(depositId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Failure<Guid>("Concurrency conflict: deposit was modified by another process. Please retry.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording deposit received for contract {ContractId}", request.ContractId);
            return Result.Failure<Guid>("Error recording deposit received");
        }
    }
}
