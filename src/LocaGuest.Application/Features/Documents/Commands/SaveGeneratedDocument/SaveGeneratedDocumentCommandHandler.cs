using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;

public class SaveGeneratedDocumentCommandHandler : IRequestHandler<SaveGeneratedDocumentCommand, Result<DocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<SaveGeneratedDocumentCommandHandler> _logger;

    public SaveGeneratedDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        INumberSequenceService numberSequenceService,
        ILogger<SaveGeneratedDocumentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<DocumentDto>> Handle(SaveGeneratedDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var effectiveOrganizationId = _orgContext.OrganizationId ?? request.OrganizationId;

            if (!effectiveOrganizationId.HasValue)
            {
                return Result.Failure<DocumentDto>("User not authenticated");
            }

            // Parse DocumentType and Category
            if (!Enum.TryParse<DocumentType>(request.Type, out var documentType))
            {
                return Result.Failure<DocumentDto>($"Invalid document type: {request.Type}");
            }

            if (!Enum.TryParse<DocumentCategory>(request.Category, out var documentCategory))
            {
                return Result.Failure<DocumentDto>($"Invalid document category: {request.Category}");
            }

            // Generate code
            var code = await _numberSequenceService.GenerateNextCodeAsync(
                effectiveOrganizationId.Value,
                EntityPrefixes.Document,
                cancellationToken);

            // Create document entity
            var document = Document.Create(
                request.FileName,
                request.FilePath,
                documentType,
                documentCategory,
                request.FileSizeBytes,
                contractId: request.ContractId, // Association au contrat si fourni
                tenantId: request.TenantId,
                propertyId: request.PropertyId,
                description: request.Description);

            // Background jobs may not have a HTTP context; still persist OrganizationId for query filters.
            document.SetOrganizationId(effectiveOrganizationId.Value);

            document.SetCode(code);

            await _unitOfWork.Documents.AddAsync(document, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Document saved: {Code} - {FileName}, ContractId={ContractId}", 
                code, 
                request.FileName, 
                request.ContractId);

            // Load names for DTO
            string? tenantName = null;
            string? propertyName = null;

            if (request.TenantId.HasValue)
            {
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId.Value, cancellationToken);
                tenantName = tenant?.FullName;
            }

            if (request.PropertyId.HasValue)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId.Value, cancellationToken);
                propertyName = property?.Name;
            }

            var dto = new DocumentDto
            {
                Id = document.Id,
                Code = document.Code,
                FileName = document.FileName,
                FilePath = document.FilePath,
                Type = document.Type.ToString(),
                Category = document.Category.ToString(),
                FileSizeBytes = document.FileSizeBytes,
                Description = document.Description,
                ExpiryDate = document.ExpiryDate,
                TenantId = document.AssociatedTenantId,
                TenantName = tenantName,
                PropertyId = document.PropertyId,
                PropertyName = propertyName,
                IsArchived = document.IsArchived,
                CreatedAt = document.CreatedAt
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document {FileName}", request.FileName);
            return Result.Failure<DocumentDto>($"Error saving document: {ex.Message}");
        }
    }
}
