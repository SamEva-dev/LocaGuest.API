using LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;
using LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;
using LocaGuest.Application.DTOs.Documents;
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

    public DocumentsController(IMediator mediator, ILogger<DocumentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
}
