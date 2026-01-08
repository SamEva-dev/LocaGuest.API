using System.Globalization;
using LocaGuest.Application.Common;
using LocaGuest.Application.Interfaces;
using LocaGuest.Domain.Aggregates.DepositAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Deposits.Queries.GetDepositReceiptByContract;

public class GetDepositReceiptByContractQueryHandler : IRequestHandler<GetDepositReceiptByContractQuery, Result<byte[]>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuittanceGeneratorService _quittanceGenerator;
    private readonly ILogger<GetDepositReceiptByContractQueryHandler> _logger;

    public GetDepositReceiptByContractQueryHandler(
        IUnitOfWork unitOfWork,
        IQuittanceGeneratorService quittanceGenerator,
        ILogger<GetDepositReceiptByContractQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _quittanceGenerator = quittanceGenerator;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(GetDepositReceiptByContractQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var deposit = await _unitOfWork.Deposits.GetByContractIdAsync(request.ContractId, cancellationToken);
            if (deposit == null)
                return Result.Failure<byte[]>("Deposit not found");

            var receiveTx = deposit.Transactions
                .Where(t => t.Kind == DepositTransactionKind.Receive)
                .OrderByDescending(t => t.DateUtc)
                .FirstOrDefault();

            if (receiveTx == null)
                return Result.Failure<byte[]>("No deposit payment found");

            var contract = await _unitOfWork.Contracts.GetByIdAsync(deposit.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<byte[]>("Contract not found");

            var tenant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterTenantId, cancellationToken);
            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);

            if (tenant == null || property == null)
                return Result.Failure<byte[]>("Locataire ou propriété introuvable");

            var culture = CultureInfo.GetCultureInfo("fr-FR");
            var monthLabel = receiveTx.DateUtc.ToString("MMMM yyyy", culture);

            var pdf = await _quittanceGenerator.GenerateQuittancePdfAsync(
                paymentType: PaymentType.Deposit,
                tenantName: tenant.FullName,
                tenantEmail: tenant.Email,
                propertyName: property.Name,
                propertyAddress: property.Address,
                propertyCity: property.City,
                amount: receiveTx.Amount,
                paymentDate: receiveTx.DateUtc,
                month: monthLabel,
                reference: receiveTx.Id.ToString(),
                cancellationToken: cancellationToken);

            return Result.Success(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deposit receipt for contract {ContractId}", request.ContractId);
            return Result.Failure<byte[]>("Error generating deposit receipt");
        }
    }
}
