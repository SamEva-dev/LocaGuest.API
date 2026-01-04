using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure<PaymentDto>("Tenant not found");
            }

            // 3. Vérifier qu'il n'existe pas déjà un paiement pour ce mois (loyer uniquement)
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

            // 4b. Parser le payment type
            if (!Enum.TryParse<PaymentType>(request.PaymentType, true, out var paymentType))
            {
                return Result.Failure<PaymentDto>("Invalid payment type");
            }

            // Pour la caution: 1 seule caution par contrat
            if (paymentType == PaymentType.Deposit)
            {
                var existingDeposit = await _unitOfWork.Payments.Query()
                    .FirstOrDefaultAsync(p => p.ContractId == request.ContractId && p.PaymentType == PaymentType.Deposit, cancellationToken);

                if (existingDeposit != null)
                {
                    return Result.Failure<PaymentDto>("A deposit payment already exists for this contract");
                }

                var depositPayment = Payment.Create(
                    tenantId: request.TenantId,
                    propertyId: request.PropertyId,
                    contractId: request.ContractId,
                    paymentType: paymentType,
                    amountDue: request.AmountDue,
                    amountPaid: request.AmountPaid,
                    expectedDate: request.ExpectedDate,
                    paymentDate: request.PaymentDate,
                    paymentMethod: paymentMethod,
                    note: request.Note);

                await _unitOfWork.Payments.AddAsync(depositPayment, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                if (depositPayment.IsPaid() && depositPayment.PaymentDate.HasValue && !depositPayment.ReceiptId.HasValue)
                {
                    await _mediator.Send(new GeneratePaymentQuittanceCommand { PaymentId = depositPayment.Id }, cancellationToken);
                }

                return Result.Success(new PaymentDto
                {
                    Id = depositPayment.Id,
                    TenantId = depositPayment.RenterTenantId,
                    PropertyId = depositPayment.PropertyId,
                    ContractId = depositPayment.ContractId,
                    PaymentType = depositPayment.PaymentType.ToString(),
                    AmountDue = depositPayment.AmountDue,
                    AmountPaid = depositPayment.AmountPaid,
                    RemainingAmount = depositPayment.GetRemainingAmount(),
                    PaymentDate = depositPayment.PaymentDate,
                    ExpectedDate = depositPayment.ExpectedDate,
                    Status = depositPayment.Status.ToString(),
                    PaymentMethod = depositPayment.PaymentMethod.ToString(),
                    Note = depositPayment.Note,
                    Month = depositPayment.Month,
                    Year = depositPayment.Year,
                    ReceiptId = depositPayment.ReceiptId,
                    InvoiceDocumentId = depositPayment.InvoiceDocumentId,
                    CreatedAt = depositPayment.CreatedAt,
                    UpdatedAt = depositPayment.LastModifiedAt,
                    TenantName = tenant.FullName
                });
            }

            // 5. Mettre à jour ou créer la RentInvoice (1 entête + lignes par occupant)
            var invoice = await _unitOfWork.RentInvoices.GetByMonthYearAsync(
                request.ContractId,
                request.ExpectedDate.Month,
                request.ExpectedDate.Year,
                cancellationToken);

            var monthStartUtc = new DateTime(request.ExpectedDate.Year, request.ExpectedDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEndUtc = monthStartUtc.AddMonths(1).AddTicks(-1);

            RentInvoiceLine? tenantLine = null;

            if (invoice == null)
            {
                var stateResult = await _effectiveContractStateResolver.ResolveForPeriodAsync(request.ContractId, monthStartUtc, monthEndUtc, cancellationToken);
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
                    lineAmounts.Add((p.RenterTenantId, p.ShareType, p.ShareValue, Math.Round(p.ShareValue, 2, MidpointRounding.AwayFromZero)));

                if (percentParticipants.Count > 0)
                {
                    foreach (var p in percentParticipants)
                    {
                        var amount = remainingForPercent * (p.ShareValue / 100m);
                        lineAmounts.Add((p.RenterTenantId, p.ShareType, p.ShareValue, Math.Round(amount, 2, MidpointRounding.AwayFromZero)));
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

                    if (la.tenantId == request.TenantId)
                    {
                        tenantLine = line;
                    }
                }
            }

            // Ensure tenant has an invoice line (must be an effective participant)
            if (tenantLine == null)
            {
                tenantLine = await _unitOfWork.RentInvoiceLines
                    .GetByInvoiceTenantAsync(invoice.Id, request.TenantId, cancellationToken);
            }

            if (tenantLine == null)
            {
                var stateResult = await _effectiveContractStateResolver.ResolveForPeriodAsync(request.ContractId, monthStartUtc, monthEndUtc, cancellationToken);
                if (stateResult.IsSuccess && stateResult.Data != null)
                {
                    var state = stateResult.Data;
                    var totalAmount = invoice.Amount;
                    var participants = state.Participants;

                    if (participants != null && participants.Count > 0)
                    {
                        var fixedParticipants = participants.Where(p => p.ShareType == BillingShareType.FixedAmount).ToList();
                        var percentParticipants = participants.Where(p => p.ShareType == BillingShareType.Percentage).ToList();

                        var fixedSum = fixedParticipants.Sum(p => p.ShareValue);
                        if (fixedSum <= totalAmount)
                        {
                            var remainingForPercent = totalAmount - fixedSum;
                            var percentSum = percentParticipants.Sum(p => p.ShareValue);
                            if (percentParticipants.Count == 0 || percentSum <= 100m)
                            {
                                var lineAmounts = new List<(Guid tenantId, BillingShareType shareType, decimal shareValue, decimal amountDue)>();

                                foreach (var p in fixedParticipants)
                                    lineAmounts.Add((p.RenterTenantId, p.ShareType, p.ShareValue, Math.Round(p.ShareValue, 2, MidpointRounding.AwayFromZero)));

                                if (percentParticipants.Count > 0)
                                {
                                    foreach (var p in percentParticipants)
                                    {
                                        var amount = remainingForPercent * (p.ShareValue / 100m);
                                        lineAmounts.Add((p.RenterTenantId, p.ShareType, p.ShareValue, Math.Round(amount, 2, MidpointRounding.AwayFromZero)));
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

                                var existingLines = await _unitOfWork.RentInvoiceLines.GetByInvoiceIdAsync(invoice.Id, cancellationToken);

                                foreach (var la in lineAmounts)
                                {
                                    if (existingLines.Any(l => l.RenterTenantId == la.tenantId))
                                        continue;

                                    var line = RentInvoiceLine.Create(
                                        rentInvoiceId: invoice.Id,
                                        tenantId: la.tenantId,
                                        amountDue: la.amountDue,
                                        shareType: la.shareType,
                                        shareValue: la.shareValue);

                                    await _unitOfWork.RentInvoiceLines.AddAsync(line, cancellationToken);

                                    if (la.tenantId == request.TenantId)
                                    {
                                        tenantLine = line;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (tenantLine == null)
            {
                return Result.Failure<PaymentDto>("Tenant is not an effective participant for this invoice period");
            }

            // 6. Créer le paiement (AmountDue vient de la ligne calculée)
            var payment = Payment.Create(
                tenantId: request.TenantId,
                propertyId: invoice.PropertyId,
                contractId: request.ContractId,
                paymentType: paymentType,
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
