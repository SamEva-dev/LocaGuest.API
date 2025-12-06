using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaymentDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Vérifier que le contrat existe
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
            {
                return Result.Failure<PaymentDto>("Contract not found");
            }

            // 2. Vérifier que le locataire existe
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure<PaymentDto>("Tenant not found");
            }

            // 3. Vérifier qu'il n'existe pas déjà un paiement pour ce mois
            var existingPayment = await _unitOfWork.Payments.GetByMonthYearAsync(
                request.ContractId,
                request.ExpectedDate.Month,
                request.ExpectedDate.Year,
                cancellationToken);

            if (existingPayment != null)
            {
                return Result.Failure<PaymentDto>(
                    $"A payment already exists for {request.ExpectedDate:MMMM yyyy}");
            }

            // 4. Parser le payment method
            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            {
                return Result.Failure<PaymentDto>("Invalid payment method");
            }

            // 5. Créer le paiement
            var payment = Payment.Create(
                tenantId: request.TenantId,
                propertyId: request.PropertyId,
                contractId: request.ContractId,
                amountDue: request.AmountDue,
                amountPaid: request.AmountPaid,
                expectedDate: request.ExpectedDate,
                paymentDate: request.PaymentDate,
                paymentMethod: paymentMethod,
                note: request.Note
            );

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            // 6. Mettre à jour ou créer la RentInvoice
            var invoice = await _unitOfWork.RentInvoices.GetByMonthYearAsync(
                request.ContractId,
                request.ExpectedDate.Month,
                request.ExpectedDate.Year,
                cancellationToken);

            if (invoice != null)
            {
                // Mettre à jour le statut de la facture
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
            else
            {
                // Créer une nouvelle facture si elle n'existe pas
                invoice = RentInvoice.Create(
                    contractId: request.ContractId,
                    tenantId: request.TenantId,
                    propertyId: request.PropertyId,
                    month: request.ExpectedDate.Month,
                    year: request.ExpectedDate.Year,
                    amount: request.AmountDue,
                    dueDate: request.ExpectedDate
                );

                invoice.UpdateStatus(payment.Status);
                if (payment.IsPaid())
                {
                    invoice.MarkAsPaid(payment.Id);
                }

                await _unitOfWork.RentInvoices.AddAsync(invoice, cancellationToken);
            }

            // 7. Sauvegarder les changements
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Payment created: {PaymentId} for tenant {TenantId}, amount: {Amount}",
                payment.Id, request.TenantId, request.AmountPaid);

            // 8. Mapper vers DTO
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
                TenantName = tenant.FullName
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for tenant {TenantId}", request.TenantId);
            return Result.Failure<PaymentDto>($"Error creating payment: {ex.Message}");
        }
    }
}
