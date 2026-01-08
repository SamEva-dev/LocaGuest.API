using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.RecordPayment;

public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordPaymentCommandHandler> _logger;

    public RecordPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RecordPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<Guid>("Contract not found");

            var amountDue = contract.Rent + contract.Charges;

            if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var paymentMethod))
            {
                paymentMethod = PaymentMethod.Cash;
            }

            var expectedDateUtc = new DateTime(
                request.PaymentDate.Year,
                request.PaymentDate.Month,
                Math.Clamp(contract.PaymentDueDay, 1, DateTime.DaysInMonth(request.PaymentDate.Year, request.PaymentDate.Month)),
                0,
                0,
                0,
                DateTimeKind.Utc);

            var payment = Payment.Create(
                tenantId: contract.RenterTenantId,
                propertyId: contract.PropertyId,
                contractId: contract.Id,
                paymentType: PaymentType.Rent,
                amountDue: amountDue,
                amountPaid: request.Amount,
                expectedDate: expectedDateUtc,
                paymentDate: request.PaymentDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.PaymentDate, DateTimeKind.Utc)
                    : request.PaymentDate.ToUniversalTime(),
                paymentMethod: paymentMethod,
                note: request.Reference);

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Payment recorded: {PaymentId} for Contract {ContractId}", 
                payment.Id, request.ContractId);

            return Result.Success(payment.Id);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error recording payment for contract {ContractId}", request.ContractId);
            return Result.Failure<Guid>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for contract {ContractId}", request.ContractId);
            return Result.Failure<Guid>($"Error recording payment: {ex.Message}");
        }
    }
}
