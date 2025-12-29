using System.Globalization;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;

public class GeneratePaymentQuittanceCommandHandler : IRequestHandler<GeneratePaymentQuittanceCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IMediator _mediator;
    private readonly IQuittanceGeneratorService _quittanceGenerator;
    private readonly ILogger<GeneratePaymentQuittanceCommandHandler> _logger;

    public GeneratePaymentQuittanceCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IMediator mediator,
        IQuittanceGeneratorService quittanceGenerator,
        ILogger<GeneratePaymentQuittanceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _mediator = mediator;
        _quittanceGenerator = quittanceGenerator;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(GeneratePaymentQuittanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return Result.Failure<Guid>("Payment not found");

            if (!payment.IsPaid() || !payment.PaymentDate.HasValue)
                return Result.Failure<Guid>("Payment must be paid to generate a quittance");

            if (payment.ReceiptId.HasValue)
                return Result.Success(payment.ReceiptId.Value);

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(payment.RenterTenantId, cancellationToken);
            var property = await _unitOfWork.Properties.GetByIdAsync(payment.PropertyId, cancellationToken);

            if (tenant == null || property == null)
                return Result.Failure<Guid>("Locataire ou propriété introuvable");

            var culture = CultureInfo.GetCultureInfo("fr-FR");
            var monthLabel = payment.ExpectedDate.ToString("MMMM yyyy", culture);

            var pdf = await _quittanceGenerator.GenerateQuittancePdfAsync(
                tenantName: tenant.FullName,
                tenantEmail: tenant.Email,
                propertyName: property.Name,
                propertyAddress: property.Address,
                propertyCity: property.City,
                amount: payment.AmountPaid,
                paymentDate: payment.PaymentDate.Value,
                month: monthLabel,
                reference: payment.Id.ToString(),
                cancellationToken: cancellationToken);

            var safeMonth = monthLabel.Replace(" ", "_");
            var fileName = $"Quittance_{safeMonth}_{tenant.Code}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var filePath = Path.Combine(Environment.CurrentDirectory, "Documents", _tenantContext.TenantId!.Value.ToString(), fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, pdf, cancellationToken);

            var saveCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = DocumentType.Quittance.ToString(),
                Category = DocumentCategory.Quittances.ToString(),
                FileSizeBytes = pdf.Length,
                ContractId = payment.ContractId,
                TenantId = payment.RenterTenantId,
                PropertyId = payment.PropertyId,
                Description = $"Quittance de loyer pour {monthLabel} - {payment.AmountPaid:N2}€"
            };

            var saved = await _mediator.Send(saveCommand, cancellationToken);
            if (!saved.IsSuccess || saved.Data == null)
                return Result.Failure<Guid>(saved.ErrorMessage ?? "Unable to save quittance document");

            payment.AttachReceipt(saved.Data.Id);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Quittance generated for payment {PaymentId} -> document {DocumentId}", payment.Id, saved.Data.Id);

            return Result.Success(saved.Data.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment quittance for payment {PaymentId}", request.PaymentId);
            return Result.Failure<Guid>($"Error generating quittance: {ex.Message}");
        }
    }
}
