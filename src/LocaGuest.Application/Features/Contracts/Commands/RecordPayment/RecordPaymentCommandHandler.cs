using LocaGuest.Application.Common;
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

            var paymentMethod = Enum.TryParse<Domain.Aggregates.ContractAggregate.ContractPaymentMethod>(
                request.Method, true, out var parsed) ? parsed : Domain.Aggregates.ContractAggregate.ContractPaymentMethod.Cash;
            
            var payment = contract.RecordPayment(
                request.Amount,
                request.PaymentDate,
                paymentMethod);

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
