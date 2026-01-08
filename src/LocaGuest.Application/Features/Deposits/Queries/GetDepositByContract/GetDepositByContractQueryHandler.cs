using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Deposits;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Deposits.Queries.GetDepositByContract;

public class GetDepositByContractQueryHandler : IRequestHandler<GetDepositByContractQuery, Result<DepositDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetDepositByContractQueryHandler> _logger;

    public GetDepositByContractQueryHandler(IUnitOfWork unitOfWork, ILogger<GetDepositByContractQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DepositDto>> Handle(GetDepositByContractQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var deposit = await _unitOfWork.Deposits.GetByContractIdAsync(request.ContractId, cancellationToken);
            if (deposit == null)
                return Result.Failure<DepositDto>("Deposit not found");

            var dto = new DepositDto
            {
                Id = deposit.Id,
                ContractId = deposit.ContractId,
                AmountExpected = deposit.AmountExpected,
                DueDate = deposit.DueDate,
                AllowInstallments = deposit.AllowInstallments,
                Status = deposit.Status.ToString(),
                TotalReceived = deposit.GetTotalReceived(),
                TotalRefunded = deposit.GetTotalRefunded(),
                TotalDeducted = deposit.GetTotalDeducted(),
                BalanceHeld = deposit.GetBalanceHeld(),
                Outstanding = deposit.GetOutstanding(),
                Transactions = deposit.Transactions
                    .OrderBy(t => t.DateUtc)
                    .Select(t => new DepositTransactionDto
                    {
                        Id = t.Id,
                        Kind = t.Kind.ToString(),
                        Amount = t.Amount,
                        DateUtc = t.DateUtc,
                        Reference = t.Reference
                    })
                    .ToList()
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deposit for contract {ContractId}", request.ContractId);
            return Result.Failure<DepositDto>("Error retrieving deposit");
        }
    }
}
