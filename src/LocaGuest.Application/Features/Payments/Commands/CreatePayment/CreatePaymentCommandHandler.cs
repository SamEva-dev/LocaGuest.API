using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly IMediator _mediator;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        IMediator mediator,
        ILogger<CreatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _mediator = mediator;
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
                request.TenantId,
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

            // 5. Mettre à jour ou créer la RentInvoice (1 entête + lignes par occupant)
            var invoice = await _unitOfWork.RentInvoices.GetByMonthYearAsync(
                request.ContractId,
                request.ExpectedDate.Month,
                request.ExpectedDate.Year,
                cancellationToken);

            var monthStartUtc = new DateTime(request.ExpectedDate.Year, request.ExpectedDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            if (invoice == null)
            {
                var stateResult = await _effectiveContractStateResolver.ResolveAsync(request.ContractId, monthStartUtc, cancellationToken);
                if (!stateResult.IsSuccess || stateResult.Data == null)
                    return Result.Failure<PaymentDto>(stateResult.ErrorMessage ?? "Unable to resolve effective contract state");

                var state = stateResult.Data;
                var totalAmount = state.Rent + state.Charges;

                var daysInMonth = DateTime.DaysInMonth(request.ExpectedDate.Year, request.ExpectedDate.Month);
                var dueDay = Math.Clamp(contract.PaymentDueDay, 1, daysInMonth);
                var dueDate = new DateTime(request.ExpectedDate.Year, request.ExpectedDate.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);

                invoice = RentInvoice.Create(
                    contractId: request.ContractId,
                    tenantId: contract.RenterTenantId,
                    propertyId: contract.PropertyId,
                    month: request.ExpectedDate.Month,
                    year: request.ExpectedDate.Year,
                    amount: totalAmount,
                    dueDate: dueDate);

                await _unitOfWork.RentInvoices.AddAsync(invoice, cancellationToken);

                // Create all invoice lines from effective participants
                var participants = state.Participants;
                if (participants == null || participants.Count == 0)
                    return Result.Failure<PaymentDto>("No effective participants for this contract/month");

                var fixedParticipants = participants.Where(p => p.ShareType == BillingShareType.FixedAmount).ToList();
                var percentParticipants = participants.Where(p => p.ShareType == BillingShareType.Percentage).ToList();

                var fixedSum = fixedParticipants.Sum(p => p.ShareValue);
                if (fixedSum > totalAmount)
                    return Result.Failure<PaymentDto>("Fixed amount shares exceed invoice total");

                var remainingForPercent = totalAmount - fixedSum;
                var percentSum = percentParticipants.Sum(p => p.ShareValue);
                if (percentParticipants.Count > 0 && percentSum > 100m)
                    return Result.Failure<PaymentDto>("Percentage shares exceed 100%");

                var lineAmounts = new List<(Guid tenantId, BillingShareType shareType, decimal shareValue, decimal amountDue)>();

                foreach (var p in fixedParticipants)
                    lineAmounts.Add((p.TenantId, p.ShareType, p.ShareValue, Math.Round(p.ShareValue, 2, MidpointRounding.AwayFromZero)));

                if (percentParticipants.Count > 0)
                {
                    foreach (var p in percentParticipants)
                    {
                        var amount = remainingForPercent * (p.ShareValue / 100m);
                        lineAmounts.Add((p.TenantId, p.ShareType, p.ShareValue, Math.Round(amount, 2, MidpointRounding.AwayFromZero)));
                    }

                    var computed = lineAmounts.Where(x => x.shareType == BillingShareType.Percentage).Sum(x => x.amountDue);
                    var diff = Math.Round(remainingForPercent - computed, 2, MidpointRounding.AwayFromZero);
                    if (diff != 0m)
                    {
                        var lastIndex = lineAmounts.FindLastIndex(x => x.shareType == BillingShareType.Percentage);
                        if (lastIndex >= 0)
                        {
                            var last = lineAmounts[lastIndex];
                            lineAmounts[lastIndex] = (last.tenantId, last.shareType, last.shareValue, last.amountDue + diff);
                        }
                    }
                }

                foreach (var la in lineAmounts)
                {
                    var line = RentInvoiceLine.Create(
                        rentInvoiceId: invoice.Id,
                        tenantId: la.tenantId,
                        amountDue: la.amountDue,
                        shareType: la.shareType,
                        shareValue: la.shareValue);

                    await _unitOfWork.RentInvoiceLines.AddAsync(line, cancellationToken);
                }
            }

            // Ensure tenant has an invoice line (must be an effective participant)
            var tenantLine = await _unitOfWork.RentInvoiceLines
                .GetByInvoiceTenantAsync(invoice.Id, request.TenantId, cancellationToken);

            if (tenantLine == null)
            {
                return Result.Failure<PaymentDto>("Tenant is not an effective participant for this invoice period");
            }

            // 6. Créer le paiement (AmountDue vient de la ligne calculée)
            var payment = Payment.Create(
                tenantId: request.TenantId,
                propertyId: invoice.PropertyId,
                contractId: request.ContractId,
                amountDue: tenantLine.AmountDue,
                amountPaid: request.AmountPaid,
                expectedDate: request.ExpectedDate,
                paymentDate: request.PaymentDate,
                paymentMethod: paymentMethod,
                note: request.Note);

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            // 7. Appliquer le paiement sur la ligne correspondante et recalculer statut entête
            tenantLine.ApplyPayment(payment.Id, request.AmountPaid, request.PaymentDate);
            var allLines = await _unitOfWork.RentInvoiceLines.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
            invoice.UpdateStatusFromLines(allLines);

            // 8. Sauvegarder les changements
            await _unitOfWork.CommitAsync(cancellationToken);

            // 9. Générer quittance automatiquement si paiement complet et pas encore de receipt
            if (payment.IsPaid() && payment.PaymentDate.HasValue && !payment.ReceiptId.HasValue)
            {
                await _mediator.Send(
                    new GeneratePaymentQuittanceCommand { PaymentId = payment.Id },
                    cancellationToken);
            }

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
