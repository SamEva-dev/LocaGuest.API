using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractTerminationEligibility;

public class GetContractTerminationEligibilityQueryHandler : IRequestHandler<GetContractTerminationEligibilityQuery, Result<ContractTerminationEligibilityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetContractTerminationEligibilityQueryHandler> _logger;

    public GetContractTerminationEligibilityQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetContractTerminationEligibilityQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ContractTerminationEligibilityDto>> Handle(GetContractTerminationEligibilityQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<ContractTerminationEligibilityDto>("Contract not found");

            if (contract.Status != ContractStatus.Active)
            {
                return Result.Success(new ContractTerminationEligibilityDto
                {
                    CanTerminate = false,
                    BlockReason = "CONTRACT_NOT_ACTIVE"
                });
            }

            var inventories = await _unitOfWork.InventoryEntries.Query()
                .Where(i => i.ContractId == contract.Id)
                .Select(i => new { EntryId = (Guid?)i.Id, i.IsFinalized })
                .FirstOrDefaultAsync(cancellationToken);

            var hasEntry = inventories != null;
            var entryId = inventories?.EntryId;

            var exit = await _unitOfWork.InventoryExits.Query()
                .Where(i => i.ContractId == contract.Id)
                .Select(i => new { ExitId = (Guid?)i.Id })
                .FirstOrDefaultAsync(cancellationToken);

            var hasExit = exit != null;
            var exitId = exit?.ExitId;

            var payments = await _unitOfWork.Payments.GetByContractIdAsync(contract.Id, cancellationToken);

            var now = DateTime.UtcNow.Date;
            var overduePayments = payments.Where(p =>
                p.Status == PaymentStatus.Late ||
                (p.Status == PaymentStatus.Partial && p.ExpectedDate.Date < now) ||
                (p.Status == PaymentStatus.Pending && p.ExpectedDate.Date < now));

            var overdueCount = overduePayments.Count();
            var outstanding = overduePayments.Sum(p => p.GetRemainingAmount());
            var paymentsUpToDate = overdueCount == 0 && outstanding <= 0;

            var dto = new ContractTerminationEligibilityDto
            {
                HasInventoryEntry = hasEntry,
                HasInventoryExit = hasExit,
                InventoryEntryId = entryId,
                InventoryExitId = exitId,
                PaymentsUpToDate = paymentsUpToDate,
                OverduePaymentsCount = overdueCount,
                OutstandingAmount = outstanding,
                CanTerminate = hasEntry && hasExit && paymentsUpToDate
            };

            if (!hasEntry)
                dto.BlockReason = "INVENTORY_ENTRY_MISSING";
            else if (!hasExit)
                dto.BlockReason = "INVENTORY_EXIT_MISSING";
            else if (!paymentsUpToDate)
                dto.BlockReason = "PAYMENTS_NOT_UP_TO_DATE";

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking termination eligibility for contract {ContractId}", request.ContractId);
            return Result.Failure<ContractTerminationEligibilityDto>("Error checking termination eligibility");
        }
    }
}
