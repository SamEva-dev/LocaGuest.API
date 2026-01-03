using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Commands.GenerateQuittance;

public class GenerateQuittanceCommandHandler : IRequestHandler<GenerateQuittanceCommand, Result<byte[]>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly IMediator _mediator;
    private readonly IQuittanceGeneratorService _quittanceGenerator;
    private readonly ILogger<GenerateQuittanceCommandHandler> _logger;

    public GenerateQuittanceCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        IMediator mediator,
        IQuittanceGeneratorService quittanceGenerator,
        ILogger<GenerateQuittanceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _mediator = mediator;
        _quittanceGenerator = quittanceGenerator;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(GenerateQuittanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = Guid.Parse(request.TenantId);
            var propertyId = Guid.Parse(request.PropertyId);

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);

            if (tenant == null || property == null)
                return Result.Failure<byte[]>("Locataire ou propriété introuvable");

            // Generate PDF using service
            var pdf = await _quittanceGenerator.GenerateQuittancePdfAsync(
                paymentType: LocaGuest.Domain.Aggregates.PaymentAggregate.PaymentType.Rent,
                tenant.FullName,
                tenant.Email,
                property.Name,
                property.Address,
                property.City,
                request.Amount,
                request.PaymentDate,
                request.Month,
                request.Reference,
                cancellationToken);

            _logger.LogInformation("Quittance generated for tenant {TenantId}, amount {Amount}", 
                tenantId, request.Amount);

            // Save to database
            var fileName = $"Quittance_{request.Month.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var filePath = Path.Combine(Environment.CurrentDirectory, "Documents", _orgContext.OrganizationId!.Value.ToString(), fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, pdf, cancellationToken);

            var saveCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = DocumentType.Quittance.ToString(),
                Category = DocumentCategory.Quittances.ToString(),
                FileSizeBytes = pdf.Length,
                TenantId = tenantId,
                PropertyId = propertyId,
                Description = $"Quittance de loyer pour {request.Month} - {request.Amount:N2}€"
            };

            await _mediator.Send(saveCommand, cancellationToken);

            return Result.Success(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quittance");
            return Result.Failure<byte[]>($"Erreur lors de la génération: {ex.Message}");
        }
    }
}
