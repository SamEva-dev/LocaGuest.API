using LocaGuest.Application.Common;
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
    private readonly ILogger<GenerateMonthlyInvoicesCommandHandler> _logger;

    public GenerateMonthlyInvoicesCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<GenerateMonthlyInvoicesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
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

                    // Calculate due date (5th of the month)
                    var dueDate = new DateTime(request.Year, request.Month, 5, 0, 0, 0, DateTimeKind.Utc);

                    // Create new invoice
                    var invoice = RentInvoice.Create(
                        contract.Id,
                        contract.RenterTenantId,
                        contract.PropertyId,
                        request.Month,
                        request.Year,
                        contract.Rent + contract.Charges,
                        dueDate
                    );

                    await _unitOfWork.RentInvoices.AddAsync(invoice, cancellationToken);
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
