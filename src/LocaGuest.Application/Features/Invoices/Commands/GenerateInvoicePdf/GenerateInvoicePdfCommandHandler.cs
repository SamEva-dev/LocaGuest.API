using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Commands.GenerateInvoicePdf;

public class GenerateInvoicePdfCommandHandler : IRequestHandler<GenerateInvoicePdfCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly IMediator _mediator;
    private readonly IInvoicePdfGeneratorService _invoicePdfGenerator;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly ILogger<GenerateInvoicePdfCommandHandler> _logger;

    public GenerateInvoicePdfCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        IMediator mediator,
        IInvoicePdfGeneratorService invoicePdfGenerator,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        ILogger<GenerateInvoicePdfCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _mediator = mediator;
        _invoicePdfGenerator = invoicePdfGenerator;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(GenerateInvoicePdfCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _unitOfWork.RentInvoices.GetByIdAsync(request.InvoiceId, cancellationToken);
            if (invoice == null)
                return Result.Failure<Guid>("Invoice not found");

            if (invoice.InvoiceDocumentId.HasValue)
                return Result.Success(invoice.InvoiceDocumentId.Value);

            var contract = await _unitOfWork.Contracts.GetByIdAsync(invoice.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<Guid>("Contract not found");

            var tenant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken);
            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);

            if (tenant == null || property == null)
                return Result.Failure<Guid>("Tenant or property not found");

            var lines = await _unitOfWork.RentInvoiceLines.GetByInvoiceIdAsync(invoice.Id, cancellationToken);

            // Minimal line breakdown (use effective state => avenants financiers)
            var monthStartUtc = new DateTime(invoice.Year, invoice.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEndUtc = monthStartUtc.AddMonths(1).AddTicks(-1);

            var stateResult = await _effectiveContractStateResolver.ResolveForPeriodAsync(contract.Id, monthStartUtc, monthEndUtc, cancellationToken);
            var state = stateResult.IsSuccess ? stateResult.Data : null;

            var rent = Math.Max(0m, state?.Rent ?? contract.Rent);
            var charges = Math.Max(0m, state?.Charges ?? contract.Charges);

            var pdfLines = new List<(string label, decimal amount)>
            {
                ("Loyer", rent)
            };

            if (charges > 0m)
                pdfLines.Add(("Charges", charges));

            var invoiceNumber = $"{contract.Code}-{invoice.Year}{invoice.Month:00}";

            var pdf = await _invoicePdfGenerator.GenerateInvoicePdfAsync(
                invoiceNumber: invoiceNumber,
                tenantName: tenant.FullName,
                tenantEmail: tenant.Email,
                propertyName: property.Name,
                propertyAddress: property.Address,
                propertyCity: property.City,
                month: invoice.Month,
                year: invoice.Year,
                dueDate: invoice.DueDate,
                totalAmount: invoice.Amount,
                lines: pdfLines,
                cancellationToken: cancellationToken);

            var orgId = ParseOrganizationIdFromContract(contract);
            if (!orgId.HasValue)
                return Result.Failure<Guid>("OrganizationId not available for invoice document generation");

            var fileName = $"Facture_{invoice.Year}{invoice.Month:00}_{tenant.Code}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var filePath = Path.Combine(Environment.CurrentDirectory, "Documents", orgId.Value.ToString(), fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, pdf, cancellationToken);

            var saveCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = DocumentType.Facture.ToString(),
                Category = DocumentCategory.Factures.ToString(),
                FileSizeBytes = pdf.Length,
                OrganizationId = orgId,
                ContractId = invoice.ContractId,
                OccupantId = tenant.Id,
                PropertyId = property.Id,
                Description = $"Facture de loyer {invoice.Month:00}/{invoice.Year} - {invoice.Amount:N2}€"
            };

            var saved = await _mediator.Send(saveCommand, cancellationToken);
            if (!saved.IsSuccess || saved.Data == null)
                return Result.Failure<Guid>(saved.ErrorMessage ?? "Unable to save invoice document");

            invoice.AttachInvoiceDocument(saved.Data.Id);
            _unitOfWork.RentInvoices.Update(invoice);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Invoice PDF generated for invoice {InvoiceId} -> document {DocumentId}", invoice.Id, saved.Data.Id);

            return Result.Success(saved.Data.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF for invoice {InvoiceId}", request.InvoiceId);
            return Result.Failure<Guid>($"Error generating invoice PDF: {ex.Message}");
        }
    }

    private Guid? ParseOrganizationIdFromContract(LocaGuest.Domain.Aggregates.ContractAggregate.Contract contract)
    {
        // Contract.OrganizationId is the multi-tenant organization id (AuditableEntity).
        if (contract.OrganizationId != Guid.Empty)
            return contract.OrganizationId;

        // Fallback to organization context if available (manual call).
        return _orgContext.OrganizationId;
    }
}
