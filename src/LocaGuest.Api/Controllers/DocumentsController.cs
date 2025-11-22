using LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;
using LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
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

    public DocumentsController(
        IMediator mediator, 
        ILogger<DocumentsController> logger,
        IContractGeneratorService contractGenerator,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _mediator = mediator;
        _logger = logger;
        _contractGenerator = contractGenerator;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
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

            var fileName = $"Contrat_{dto.ContractType}_{DateTime.UtcNow:yyyy-MM-dd}.pdf";

            _logger.LogInformation(
                "Contract generated successfully: {FileName}, Size={Size}KB",
                fileName,
                pdfBytes.Length / 1024);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contract for Tenant={TenantId}, Property={PropertyId}", 
                dto.TenantId, dto.PropertyId);
            return StatusCode(500, new { message = "Error generating contract", error = ex.Message });
        }
    }
}
