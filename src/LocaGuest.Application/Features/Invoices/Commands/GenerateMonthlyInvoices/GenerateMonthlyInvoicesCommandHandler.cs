using LocaGuest.Application.Common;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Commands.GenerateMonthlyInvoices;

public class GenerateMonthlyInvoicesCommandHandler : IRequestHandler<GenerateMonthlyInvoicesCommand, Result<GenerateInvoicesResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly ILogger<GenerateMonthlyInvoicesCommandHandler> _logger;

    public GenerateMonthlyInvoicesCommandHandler(
        IUnitOfWork unitOfWork,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        ILogger<GenerateMonthlyInvoicesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _logger = logger;
    }

    public async Task<Result<GenerateInvoicesResultDto>> Handle(GenerateMonthlyInvoicesCommand request, CancellationToken cancellationToken)
    {
        var generatedCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        try
        {
            // Get all active contracts for the specified month
            var targetDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active &&
                           c.StartDate <= targetDate &&
                           c.EndDate >= targetDate)
                .ToListAsync(cancellationToken);

            foreach (var contract in activeContracts)
            {
                try
                {
                    var effectiveStateResult = await _effectiveContractStateResolver
                        .ResolveAsync(contract.Id, targetDate, cancellationToken);

                    if (!effectiveStateResult.IsSuccess || effectiveStateResult.Data == null)
                        throw new InvalidOperationException(effectiveStateResult.ErrorMessage ?? "Unable to resolve effective contract state");

                    var state = effectiveStateResult.Data;

                    // Check if invoice already exists for this contract/month/year
                    var existingInvoice = await _unitOfWork.RentInvoices
                        .GetByMonthYearAsync(contract.Id, request.Month, request.Year, cancellationToken);

                    if (existingInvoice != null)
                    {
                        skippedCount++;
                        _logger.LogInformation("Invoice already exists for contract {ContractId} for {Month}/{Year}", 
                            contract.Id, request.Month, request.Year);
                        continue;
                    }

                    // Calculate due date (contract.PaymentDueDay of the month)
                    var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
                    var dueDay = Math.Clamp(contract.PaymentDueDay, 1, daysInMonth);
                    var dueDate = new DateTime(request.Year, request.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);

                    var totalAmount = state.Rent + state.Charges;

                    // Create new invoice (header)
                    var invoice = RentInvoice.Create(
                        contract.Id,
                        contract.RenterTenantId,
                        contract.PropertyId,
                        request.Month,
                        request.Year,
                        totalAmount,
                        dueDate
                    );

                    // Ensure multi-tenant isolation tenant id is set even in background jobs
                    ((LocaGuest.Domain.Common.AuditableEntity)invoice).SetOrganizationId(contract.OrganizationId);

                    await _unitOfWork.RentInvoices.AddAsync(invoice, cancellationToken);

                    // Create invoice line(s) - per effective participant
                    var participants = state.Participants;
                    if (participants == null || participants.Count == 0)
                        throw new InvalidOperationException("No effective participants for invoice generation");

                    var fixedParticipants = participants.Where(p => p.ShareType == BillingShareType.FixedAmount).ToList();
                    var percentParticipants = participants.Where(p => p.ShareType == BillingShareType.Percentage).ToList();

                    var fixedSum = fixedParticipants.Sum(p => p.ShareValue);
                    if (fixedSum < 0m)
                        throw new InvalidOperationException("Invalid fixed share sum");

                    if (fixedSum > totalAmount)
                        throw new InvalidOperationException("Fixed amount shares exceed invoice total");

                    var remainingForPercent = totalAmount - fixedSum;
                    var percentSum = percentParticipants.Sum(p => p.ShareValue);

                    if (percentParticipants.Count > 0)
                    {
                        if (percentSum <= 0m)
                            throw new InvalidOperationException("Percentage shares must sum to a positive value");

                        if (percentSum > 100m)
                            throw new InvalidOperationException("Percentage shares exceed 100%");
                    }

                    var lineAmounts = new List<(Guid tenantId, BillingShareType shareType, decimal shareValue, decimal amountDue)>();

                    foreach (var p in fixedParticipants)
                    {
                        lineAmounts.Add((p.RenterTenantId, p.ShareType, p.ShareValue, Math.Round(p.ShareValue, 2, MidpointRounding.AwayFromZero)));
                    }

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
                        if (la.amountDue < 0m)
                            throw new InvalidOperationException("Computed line amount cannot be negative");

                        var line = RentInvoiceLine.Create(
                            rentInvoiceId: invoice.Id,
                            tenantId: la.tenantId,
                            amountDue: la.amountDue,
                            shareType: la.shareType,
                            shareValue: la.shareValue);

                        // Ensure multi-tenant isolation tenant id is set even in background jobs
                        ((LocaGuest.Domain.Common.AuditableEntity)line).SetOrganizationId(contract.OrganizationId);

                        await _unitOfWork.RentInvoiceLines.AddAsync(line, cancellationToken);
                    }

                    var allLinesAmount = lineAmounts.Sum(x => x.amountDue);
                    if (Math.Round(allLinesAmount - totalAmount, 2, MidpointRounding.AwayFromZero) != 0m)
                        throw new InvalidOperationException("Sum of invoice lines does not match invoice total");

                    generatedCount++;

                    _logger.LogInformation("Invoice generated for contract {ContractId} for {Month}/{Year}", 
                        contract.Id, request.Month, request.Year);
                }
                catch (Exception ex)
                {
                    var error = $"Erreur lors de la génération de la facture pour le contrat {contract.Code}: {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, "Error generating invoice for contract {ContractId}", contract.Id);
                }
            }

            if (generatedCount > 0)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            var result = new GenerateInvoicesResultDto(generatedCount, skippedCount, errors);
            return Result<GenerateInvoicesResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly invoices for {Month}/{Year}", request.Month, request.Year);
            return Result<GenerateInvoicesResultDto>.Failure<GenerateInvoicesResultDto>($"Erreur lors de la génération des factures: {ex.Message}");
        }
    }
}
