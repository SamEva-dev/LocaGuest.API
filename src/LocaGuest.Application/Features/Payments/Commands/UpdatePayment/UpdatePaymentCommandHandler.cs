using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Commands.UpdatePayment;

public class UpdatePaymentCommandHandler : IRequestHandler<UpdatePaymentCommand, Result<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePaymentCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IMediator _mediator;

    public UpdatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdatePaymentCommandHandler> logger,
        IEmailService emailService,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
        _mediator = mediator;
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
                // Update corresponding invoice line for this tenant
                var line = await _unitOfWork.RentInvoiceLines
                    .GetByInvoiceTenantAsync(invoice.Id, payment.RenterTenantId, cancellationToken);

                if (line != null)
                {
                    line.ApplyPayment(payment.Id, payment.AmountPaid, payment.PaymentDate);
                }

                var lines = await _unitOfWork.RentInvoiceLines.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
                invoice.UpdateStatusFromLines(lines);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            // Generate quittance if payment is fully paid and no receipt exists yet
            if (payment.IsPaid() && payment.PaymentDate.HasValue && !payment.ReceiptId.HasValue)
            {
                await _mediator.Send(
                    new GeneratePaymentQuittanceCommand { PaymentId = payment.Id },
                    cancellationToken);
            }

            _logger.LogInformation("Payment updated: {PaymentId}", paymentId);

            // Récupérer le tenant pour enrichir le DTO
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(payment.RenterTenantId, cancellationToken);

            // Send email notification if payment is fully paid
            if (payment.IsPaid() && tenant?.Email != null && payment.PaymentDate.HasValue)
            {
                try
                {
                    // Check if tenant has PaymentReceived notification enabled
                    var notificationSettings = await _unitOfWork.NotificationSettings
                        .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                    if (notificationSettings?.PaymentReceived == true)
                    {
                        await _emailService.SendPaymentReceivedEmailAsync(
                            tenant.Email,
                            tenant.FullName,
                            payment.AmountPaid,
                            payment.PaymentDate.Value);

                        _logger.LogInformation("Payment received email sent to {Email}", tenant.Email);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send payment received email");
                    // Don't fail the payment update if email fails
                }
            }

            var dto = new PaymentDto
            {
                Id = payment.Id,
                TenantId = payment.RenterTenantId,
                PropertyId = payment.PropertyId,
                ContractId = payment.ContractId,
                PaymentType = payment.PaymentType.ToString(),
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
                InvoiceDocumentId = payment.InvoiceDocumentId,
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
