using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Commands.UpdatePayment;

public class UpdatePaymentCommandHandler : IRequestHandler<UpdatePaymentCommand, Result<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePaymentCommandHandler> _logger;

    public UpdatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaymentDto>> Handle(UpdatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.PaymentId, out var paymentId))
            {
                return Result.Failure<PaymentDto>("Invalid payment ID format");
            }

            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                return Result.Failure<PaymentDto>("Payment not found");
            }

            // Parser payment method si fourni
            PaymentMethod? paymentMethod = null;
            if (!string.IsNullOrEmpty(request.PaymentMethod))
            {
                if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                {
                    return Result.Failure<PaymentDto>("Invalid payment method");
                }
                paymentMethod = method;
            }

            // Mettre à jour le paiement
            payment.UpdatePayment(
                amountPaid: request.AmountPaid,
                paymentDate: request.PaymentDate,
                paymentMethod: paymentMethod,
                note: request.Note
            );

            // Mettre à jour la facture associée
            var invoice = await _unitOfWork.RentInvoices.GetByMonthYearAsync(
                payment.ContractId,
                payment.Month,
                payment.Year,
                cancellationToken);

            if (invoice != null)
            {
                invoice.UpdateStatus(payment.Status);
                if (payment.IsPaid())
                {
                    invoice.MarkAsPaid(payment.Id);
                }
                else if (payment.Status == PaymentStatus.Partial)
                {
                    invoice.MarkAsPartial(payment.Id);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Payment updated: {PaymentId}", paymentId);

            // Récupérer le tenant pour enrichir le DTO
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(payment.TenantId, cancellationToken);

            var dto = new PaymentDto
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                PropertyId = payment.PropertyId,
                ContractId = payment.ContractId,
                AmountDue = payment.AmountDue,
                AmountPaid = payment.AmountPaid,
                RemainingAmount = payment.GetRemainingAmount(),
                PaymentDate = payment.PaymentDate,
                ExpectedDate = payment.ExpectedDate,
                Status = payment.Status.ToString(),
                PaymentMethod = payment.PaymentMethod.ToString(),
                Note = payment.Note,
                Month = payment.Month,
                Year = payment.Year,
                ReceiptId = payment.ReceiptId,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.LastModifiedAt,
                TenantName = tenant?.FullName
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", request.PaymentId);
            return Result.Failure<PaymentDto>($"Error updating payment: {ex.Message}");
        }
    }
}
