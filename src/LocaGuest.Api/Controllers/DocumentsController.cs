using LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;
using LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;
using LocaGuest.Application.Features.Documents.Queries.GetTenantDocuments;
using LocaGuest.Application.Features.Documents.Queries.GetAllDocuments;
using LocaGuest.Application.Features.Documents.Queries.ExportDocumentsZip;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Application.Features.Documents.Commands.GenerateQuittance;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IContractGeneratorService _contractGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        IMediator mediator, 
        ILogger<DocumentsController> logger,
        IContractGeneratorService contractGenerator,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _logger = logger;
        _contractGenerator = contractGenerator;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _environment = environment;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDocumentStats()
    {
        var query = new GetDocumentStatsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllDocuments()
    {
        var query = new GetAllDocumentsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var query = new GetDocumentTemplatesQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDocument([FromBody] GenerateDocumentRequest request)
    {
        // Simulation de génération
        var generatedDoc = new GeneratedDocumentDto
        {
            Id = Guid.NewGuid(),
            DocumentType = request.TemplateType,
            PropertyName = "Appartement Centre Ville",
            TenantName = "Jean Dupont",
            GeneratedAt = DateTime.UtcNow,
            FileName = $"{request.TemplateType}_{DateTime.UtcNow:yyyyMMdd}.pdf"
        };

        return Ok(generatedDoc);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentDocuments()
    {
        // Simulation de documents récents
        var documents = new List<GeneratedDocumentDto>
        {
            new GeneratedDocumentDto
            {
                Id = Guid.NewGuid(),
                DocumentType = "Bail de location",
                PropertyName = "Appartement Centre Ville",
                TenantName = "Jean Dupont",
                GeneratedAt = DateTime.UtcNow.AddDays(-1),
                FileName = "Bail_Dupont_Jean_20241108.pdf"
            },
            new GeneratedDocumentDto
            {
                Id = Guid.NewGuid(),
                DocumentType = "Quittance",
                PropertyName = "Studio Quartier Latin",
                TenantName = "Paul Martin",
                GeneratedAt = DateTime.UtcNow.AddDays(-2),
                FileName = "Quittance_Janvier_2024.pdf"
            },
            new GeneratedDocumentDto
            {
                Id = Guid.NewGuid(),
                DocumentType = "État des lieux",
                PropertyName = "2 pièces Montmartre",
                TenantName = "Sophie Bernard",
                GeneratedAt = DateTime.UtcNow.AddDays(-5),
                FileName = "Etat_lieux_sortie_20241105.pdf"
            }
        };

        return await Task.FromResult(Ok(documents));
    }

    [HttpPost("generate-contract")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateContract(
        [FromBody] GenerateContractDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant context
            if (!_tenantContext.IsAuthenticated || _tenantContext.TenantId == null)
            {
                _logger.LogWarning("Unauthorized contract generation attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Load tenant
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(dto.TenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", dto.TenantId);
                return NotFound(new { message = "Tenant not found" });
            }

            // Load property
            var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId, cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Property not found: {PropertyId}", dto.PropertyId);
                return NotFound(new { message = "Property not found" });
            }

            // Get current user info from JWT claims
            var firstName = User.FindFirst("given_name")?.Value ?? User.FindFirst("FirstName")?.Value ?? "";
            var lastName = User.FindFirst("family_name")?.Value ?? User.FindFirst("LastName")?.Value ?? "";
            var email = User.FindFirst("email")?.Value ?? "";
            var phone = User.FindFirst("phone_number")?.Value ?? User.FindFirst("PhoneNumber")?.Value;

            var currentUserFullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(currentUserFullName))
            {
                currentUserFullName = email; // Fallback to email
            }

            // Generate PDF
            _logger.LogInformation(
                "Generating contract: Type={ContractType}, Tenant={TenantId}, Property={PropertyId}, User={UserName}",
                dto.ContractType,
                dto.TenantId,
                dto.PropertyId,
                currentUserFullName);

            var pdfBytes = await _contractGenerator.GenerateContractPdfAsync(
                tenant,
                property,
                currentUserFullName,
                email,
                phone,
                dto,
                cancellationToken);

            var fileName = $"Contrat_{dto.ContractType}_{DateTime.UtcNow:yyyy-MM-dd}_{Guid.NewGuid():N}.pdf";

            // Save file to disk
            var documentsPath = Path.Combine(_environment.ContentRootPath, "Documents", _tenantContext.TenantId!.Value.ToString());
            Directory.CreateDirectory(documentsPath);
            var filePath = Path.Combine(documentsPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);

            _logger.LogInformation(
                "Contract PDF saved: {FilePath}, Size={Size}KB",
                filePath,
                pdfBytes.Length / 1024);

            // Save document metadata to database
            var documentType = dto.ContractType switch
            {
                "Bail" => DocumentType.Bail,
                "Colocation" => DocumentType.Colocation,
                _ => DocumentType.Bail
            };

            var saveDocumentCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = documentType.ToString(),
                Category = DocumentCategory.Contrats.ToString(),
                FileSizeBytes = pdfBytes.Length,
                TenantId = dto.TenantId,
                PropertyId = dto.PropertyId,
                Description = $"Contrat {dto.ContractType} généré automatiquement"
            };

            var saveResult = await _mediator.Send(saveDocumentCommand, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                _logger.LogWarning("Failed to save document metadata: {Error}", saveResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("Document metadata saved: {Code}", saveResult.Data!.Code);
            }

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contract for Tenant={TenantId}, Property={PropertyId}", 
                dto.TenantId, dto.PropertyId);
            return StatusCode(500, new { message = "Error generating contract", error = ex.Message });
        }
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<IActionResult> GetTenantDocuments(string tenantId)
    {
        var query = new GetTenantDocumentsQuery { TenantId = tenantId };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("property/{propertyId}")]
    public async Task<IActionResult> GetPropertyDocuments(string propertyId)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || _tenantContext.TenantId == null)
                return Unauthorized(new { message = "User not authenticated" });

            var documents = await _unitOfWork.Documents.GetByPropertyIdAsync(Guid.Parse(propertyId));
            
            // Group by category
            var groupedDocs = documents
                .GroupBy(d => d.Category)
                .Select(g => new
                {
                    Category = g.Key.ToString(),
                    Count = g.Count(),
                    Documents = g.Select(d => new
                    {
                        d.Id,
                        d.Code,
                        d.FileName,
                        d.Type,
                        Category = d.Category.ToString(),
                        d.FileSizeBytes,
                        d.CreatedAt,
                        d.Description,
                        d.TenantId,
                        d.PropertyId
                    }).ToList()
                })
                .ToList();

            return Ok(groupedDocs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property documents {PropertyId}", propertyId);
            return StatusCode(500, new { message = "Error getting property documents", error = ex.Message });
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string type, [FromForm] string category, [FromForm] string? tenantId, [FromForm] string? propertyId, [FromForm] string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!_tenantContext.IsAuthenticated || _tenantContext.TenantId == null)
                return Unauthorized(new { message = "User not authenticated" });

            // Save file to disk
            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var documentsPath = Path.Combine(_environment.ContentRootPath, "Documents", _tenantContext.TenantId!.Value.ToString());
            Directory.CreateDirectory(documentsPath);
            var filePath = Path.Combine(documentsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File uploaded: {FilePath}, Size={Size}KB", filePath, file.Length / 1024);

            // Save metadata to database
            var saveCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = type,
                Category = category,
                FileSizeBytes = file.Length,
                TenantId = string.IsNullOrEmpty(tenantId) ? null : Guid.Parse(tenantId),
                PropertyId = string.IsNullOrEmpty(propertyId) ? null : Guid.Parse(propertyId),
                Description = description
            };

            var result = await _mediator.Send(saveCommand);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { message = "Error uploading document", error = ex.Message });
        }
    }

    [HttpGet("download/{documentId}")]
    public async Task<IActionResult> DownloadDocument(string documentId)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(Guid.Parse(documentId));
            if (document == null)
                return NotFound(new { message = "Document not found" });

            if (!System.IO.File.Exists(document.FilePath))
                return NotFound(new { message = "File not found on disk" });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);
            var contentType = document.FileName.EndsWith(".pdf") ? "application/pdf" : "application/octet-stream";

            return File(fileBytes, contentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error downloading document", error = ex.Message });
        }
    }

    [HttpDelete("{documentId}/dissociate")]
    public async Task<IActionResult> DissociateDocument(string documentId)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(Guid.Parse(documentId));
            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Archive the document (dissociate but keep file)
            document.Archive();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Document archived: {Code}", document.Code);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dissociating document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error dissociating document", error = ex.Message });
        }
    }

    [HttpGet("tenant/{tenantId}/export-zip")]
    public async Task<IActionResult> ExportDocumentsZip(string tenantId)
    {
        try
        {
            var query = new ExportDocumentsZipQuery { TenantId = tenantId };
            var zipBytes = await _mediator.Send(query);

            if (zipBytes.Length == 0)
                return NotFound(new { message = "No documents found for this tenant" });

            var fileName = $"Documents_Locataire_{DateTime.UtcNow:yyyy-MM-dd}.zip";
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting documents ZIP for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Error exporting documents", error = ex.Message });
        }
    }

    [HttpPost("generate-quittance")]
    public async Task<IActionResult> GenerateQuittance([FromBody] GenerateQuittanceCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            
            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            var fileName = $"Quittance_{command.Month.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            return File(result.Data!, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quittance");
            return StatusCode(500, new { message = "Error generating quittance", error = ex.Message });
        }
    }
}
