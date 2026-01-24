using LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;
using LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;
using LocaGuest.Application.Features.Documents.Queries.GetOccupantDocuments;
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
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Api.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using LocaGuest.Application.Services;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestUnit = QuestPDF.Infrastructure.Unit;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IContractGeneratorService _contractGenerator;
    private readonly IPropertySheetGeneratorService _propertySheetGenerator;
    private readonly IOccupantSheetGeneratorService _occupantSheetGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly IOrganizationContext _orgContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;

    public DocumentsController(
        IMediator mediator, 
        ILogger<DocumentsController> logger,
        IContractGeneratorService contractGenerator,
        IPropertySheetGeneratorService propertySheetGenerator,
        IOccupantSheetGeneratorService occupantSheetGenerator,
        IUnitOfWork unitOfWork,
        ILocaGuestReadDbContext readDb,
        IOrganizationContext orgContext,
        IWebHostEnvironment environment,
        IEffectiveContractStateResolver effectiveContractStateResolver)
    {
        _mediator = mediator;
        _logger = logger;
        _contractGenerator = contractGenerator;
        _propertySheetGenerator = propertySheetGenerator;
        _occupantSheetGenerator = occupantSheetGenerator;
        _unitOfWork = unitOfWork;
        _readDb = readDb;
        _orgContext = orgContext;
        _environment = environment;
        _effectiveContractStateResolver = effectiveContractStateResolver;
    }

    public sealed class GenerateAddendumRequest
    {
        public Guid AddendumId { get; set; }
    }

    [HttpGet("property/{propertyId:guid}/sheet")]
    [Authorize(Policy = Permissions.PropertiesRead)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GeneratePropertySheet(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);
            if (property == null)
                return NotFound(new { message = "Property not found" });

            if (property.OrganizationId != _orgContext.OrganizationId.Value)
                return NotFound(new { message = "Property not found" });

            var firstName = User.FindFirst("given_name")?.Value ?? User.FindFirst("FirstName")?.Value ?? "";
            var lastName = User.FindFirst("family_name")?.Value ?? User.FindFirst("LastName")?.Value ?? "";
            var email = User.FindFirst("email")?.Value ?? "";
            var phone = User.FindFirst("phone_number")?.Value ?? User.FindFirst("PhoneNumber")?.Value;
            var currentUserFullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(currentUserFullName))
                currentUserFullName = email;

            var pdfBytes = await _propertySheetGenerator.GeneratePropertySheetPdfAsync(
                property,
                currentUserFullName,
                email,
                phone,
                cancellationToken);

            var fileName = SanitizeFileName($"Fiche_Bien_{property.Code}_{DateTime.UtcNow:yyyy-MM-dd}.pdf");
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating property sheet for {PropertyId}", propertyId);
            return StatusCode(500, new { message = "Error generating property sheet", error = ex.Message });
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return $"file_{Guid.NewGuid():N}";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(safeName.Length);
        foreach (var ch in safeName)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return sb.ToString().TrimEnd('.');
    }

    [HttpGet("occupant/{occupantId:guid}/sheet")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateOccupantSheet(
        Guid occupantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(occupantId, cancellationToken);
            if (occupant == null)
                return NotFound(new { message = "Occupant not found" });

            var property = occupant.PropertyId.HasValue
                ? await _unitOfWork.Properties.GetByIdAsync(occupant.PropertyId.Value, cancellationToken)
                : null;

            var firstName = User.FindFirst("given_name")?.Value ?? User.FindFirst("FirstName")?.Value ?? "";
            var lastName = User.FindFirst("family_name")?.Value ?? User.FindFirst("LastName")?.Value ?? "";
            var email = User.FindFirst("email")?.Value ?? "";
            var phone = User.FindFirst("phone_number")?.Value ?? User.FindFirst("PhoneNumber")?.Value;
            var currentUserFullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(currentUserFullName))
                currentUserFullName = email;

            var pdfBytes = await _occupantSheetGenerator.GenerateOccupantSheetPdfAsync(
                occupant,
                property,
                currentUserFullName,
                email,
                phone,
                cancellationToken);

            var fileName = SanitizeFileName($"Fiche_Occupant_{occupant.Code}_{DateTime.UtcNow:yyyy-MM-dd}.pdf");
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating occupant sheet for {OccupantId}", occupantId);
            return StatusCode(500, new { message = "Error generating occupant sheet", error = ex.Message });
        }
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
    [Authorize(Policy = Permissions.DocumentsWrite)]
    public async Task<IActionResult> GenerateDocument([FromBody] GenerateDocumentRequest request)
    {
        // Simulation de génération
        var generatedDoc = new GeneratedDocumentDto
        {
            Id = Guid.NewGuid(),
            DocumentType = request.TemplateType,
            PropertyName = "Appartement Centre Ville",
            OccupantName = "Jean Dupont",
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
                OccupantName = "Jean Dupont",
                GeneratedAt = DateTime.UtcNow.AddDays(-1),
                FileName = "Bail_Dupont_Jean_20241108.pdf"
            },
            new GeneratedDocumentDto
            {
                Id = Guid.NewGuid(),
                DocumentType = "Quittance",
                PropertyName = "Studio Quartier Latin",
                OccupantName = "Paul Martin",
                GeneratedAt = DateTime.UtcNow.AddDays(-2),
                FileName = "Quittance_Janvier_2024.pdf"
            },
            new GeneratedDocumentDto
            {
                Id = Guid.NewGuid(),
                DocumentType = "État des lieux",
                PropertyName = "2 pièces Montmartre",
                OccupantName = "Sophie Bernard",
                GeneratedAt = DateTime.UtcNow.AddDays(-5),
                FileName = "Etat_lieux_sortie_20241105.pdf"
            }
        };

        return await Task.FromResult(Ok(documents));
    }

    [HttpPost("generate-contract")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
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
            // Validate occupant context
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
            {
                _logger.LogWarning("Unauthorized contract generation attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Load occupant
            var occupant = await _unitOfWork.Occupants.GetByIdAsync(dto.OccupantId, cancellationToken);
            if (occupant == null)
            {
                _logger.LogWarning("Occupant not found: {OccupantId}", dto.OccupantId);
                return NotFound(new { message = "Occupant not found" });
            }

            // Load property
            var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId, cancellationToken);
            if (property == null)
            {
                _logger.LogWarning("Property not found: {PropertyId}", dto.PropertyId);
                return NotFound(new { message = "Property not found" });
            }

            if (dto.ContractId.HasValue)
            {
                var effectiveResult = await _effectiveContractStateResolver.ResolveAsync(
                    dto.ContractId.Value,
                    DateTime.UtcNow,
                    cancellationToken);

                if (effectiveResult.IsSuccess && effectiveResult.Data != null)
                {
                    var s = effectiveResult.Data;
                    dto = dto with
                    {
                        EndDate = s.EndDate.ToString("O"),
                        Rent = s.Rent,
                        Charges = s.Charges,
                        AdditionalClauses = s.CustomClauses
                    };
                }
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
                "Generating contract: Type={ContractType}, Occupant={OccupantId}, Property={PropertyId}, User={UserName}",
                dto.ContractType,
                dto.OccupantId,
                dto.PropertyId,
                currentUserFullName);

            var pdfBytes = await _contractGenerator.GenerateContractPdfAsync(
                occupant,
                property,
                currentUserFullName,
                email,
                phone,
                dto,
                cancellationToken);

            var fileName = SanitizeFileName($"Contrat_{dto.ContractType}_{DateTime.UtcNow:yyyy-MM-dd}_{Guid.NewGuid():N}.pdf");

            // Save file to disk
            var documentsPath = Path.Combine(_environment.ContentRootPath, "Documents", _orgContext.OrganizationId!.Value.ToString());
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
                "Avenant" => DocumentType.Avenant,
                "EtatDesLieuxEntree" or "Etat_Lieux_Entree" => DocumentType.EtatDesLieuxEntree,
                "EtatDesLieuxSortie" or "Etat_Lieux_Sortie" => DocumentType.EtatDesLieuxSortie,
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
                ContractId = dto.ContractId, // NOUVEAU: Association au contrat
                OccupantId = dto.OccupantId,
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

                // NOUVEAU: Associer le document au contrat
                if (dto.ContractId.HasValue && saveResult.Data?.Id != null)
                {
                    var contract = await _unitOfWork.Contracts.GetByIdAsync(dto.ContractId.Value, cancellationToken);
                    if (contract != null)
                    {
                        contract.MarkDocumentProvided(documentType);
                        await _unitOfWork.CommitAsync(cancellationToken);

                        _logger.LogInformation(
                            "Document {DocumentId} associated with Contract {ContractId}, Status={Status}",
                            saveResult.Data.Id,
                            contract.Id,
                            contract.Status);
                    }
                    else
                    {
                        _logger.LogWarning("Contract {ContractId} not found for document association", dto.ContractId.Value);
                    }
                }
            }

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contract for Occupant={OccupantId}, Property={PropertyId}",
                dto.OccupantId, dto.PropertyId);
            return StatusCode(500, new { message = "Error generating contract", error = ex.Message });
        }
    }

    [HttpPost("generate-addendum")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateAddendum(
        [FromBody] GenerateAddendumRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var addendum = await _unitOfWork.Addendums.GetByIdAsync(request.AddendumId, cancellationToken);
            if (addendum == null)
                return NotFound(new { message = "Addendum not found" });

            var contract = await _unitOfWork.Contracts.GetByIdAsync(addendum.ContractId, cancellationToken);
            if (contract == null)
                return NotFound(new { message = "Contract not found" });

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken);
            if (occupant == null)
                return NotFound(new { message = "Occupant not found" });

            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return NotFound(new { message = "Property not found" });

            var firstName = User.FindFirst("given_name")?.Value ?? User.FindFirst("FirstName")?.Value ?? "";
            var lastName = User.FindFirst("family_name")?.Value ?? User.FindFirst("LastName")?.Value ?? "";
            var email = User.FindFirst("email")?.Value ?? "";
            var currentUserFullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(currentUserFullName))
                currentUserFullName = email;

            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, QuestUnit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("AVENANT AU CONTRAT").FontSize(18).Bold();
                        col.Item().Text($"Contrat: {contract.Code}  |  Type: {addendum.Type}").FontSize(11);
                        col.Item().Text($"Date d'effet: {addendum.EffectiveDate:yyyy-MM-dd}").FontSize(11);
                        col.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text("Parties").FontSize(14).Bold();
                        col.Item().Text($"Locataire: {occupant.FullName} ({occupant.Email})");
                        col.Item().Text($"Bien: {property.Name} ({property.Code})");

                        col.Item().PaddingTop(10).Text("Motif").FontSize(14).Bold();
                        col.Item().Text(addendum.Reason);

                        col.Item().PaddingTop(10).Text("Description").FontSize(14).Bold();
                        col.Item().Text(addendum.Description);

                        col.Item().PaddingTop(10).Text("Modifications").FontSize(14).Bold();
                        col.Item().Column(c2 =>
                        {
                            c2.Spacing(4);
                            if (addendum.NewRent.HasValue || addendum.NewCharges.HasValue)
                                c2.Item().Text($"Financier: Loyer {addendum.OldRent:0.##}€ → {addendum.NewRent:0.##}€ ; Charges {addendum.OldCharges:0.##}€ → {addendum.NewCharges:0.##}€");
                            if (addendum.NewEndDate.HasValue)
                                c2.Item().Text($"Durée: Fin {addendum.OldEndDate:yyyy-MM-dd} → {addendum.NewEndDate:yyyy-MM-dd}");
                            if (addendum.NewRoomId.HasValue)
                                c2.Item().Text($"Chambre: {addendum.OldRoomId} → {addendum.NewRoomId}");
                            if (!string.IsNullOrWhiteSpace(addendum.NewClauses))
                                c2.Item().Text("Clauses: voir texte ci-dessous");
                            if (addendum.Type == AddendumType.Occupants)
                                c2.Item().Text("Occupants: changements enregistrés dans le système");
                        });

                        if (!string.IsNullOrWhiteSpace(addendum.NewClauses))
                        {
                            col.Item().PaddingTop(10).Text("Nouvelles clauses").FontSize(14).Bold();
                            col.Item().Text(addendum.NewClauses!);
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Généré le {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC par {currentUserFullName}")
                        .FontSize(9);
                });
            }).GeneratePdf();

            var fileName = SanitizeFileName($"Avenant_{contract.Code}_{DateTime.UtcNow:yyyy-MM-dd}_{Guid.NewGuid():N}.pdf");
            var documentsPath = Path.Combine(_environment.ContentRootPath, "Documents", _orgContext.OrganizationId!.Value.ToString());
            Directory.CreateDirectory(documentsPath);
            var filePath = Path.Combine(documentsPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);

            var saveDocumentCommand = new SaveGeneratedDocumentCommand
            {
                FileName = fileName,
                FilePath = filePath,
                Type = DocumentType.Avenant.ToString(),
                Category = DocumentCategory.Contrats.ToString(),
                FileSizeBytes = pdfBytes.Length,
                ContractId = contract.Id,
                OccupantId = contract.RenterOccupantId,
                PropertyId = contract.PropertyId,
                Description = $"Avenant généré - {addendum.Type} - {addendum.EffectiveDate:yyyy-MM-dd}"
            };

            var saved = await _mediator.Send(saveDocumentCommand, cancellationToken);
            if (!saved.IsSuccess || saved.Data == null)
                return BadRequest(new { message = saved.ErrorMessage ?? "Unable to save addendum document" });

            var currentDocIds = new List<Guid>();
            if (!string.IsNullOrWhiteSpace(addendum.AttachedDocumentIds))
            {
                try
                {
                    currentDocIds = JsonSerializer.Deserialize<List<Guid>>(addendum.AttachedDocumentIds) ?? new List<Guid>();
                }
                catch
                {
                    currentDocIds = new List<Guid>();
                }
            }

            if (!currentDocIds.Contains(saved.Data.Id))
                currentDocIds.Insert(0, saved.Data.Id);

            addendum.UpdateDocuments(currentDocIds);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Ok(saved.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating addendum {AddendumId}", request.AddendumId);
            return StatusCode(500, new { message = "Error generating addendum", error = ex.Message });
        }
    }

    [HttpGet("occupant/{occupantId}")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    public async Task<IActionResult> GetOccupantDocuments(string occupantId)
    {
        var query = new GetOccupantDocumentsQuery { OccupantId = occupantId };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("property/{propertyId}")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    public async Task<IActionResult> GetPropertyDocuments(string propertyId)
    {
        try
        {
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
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
                        d.OrganizationId,
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
    [Authorize(Policy = Permissions.DocumentsWrite)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromForm] string type,
        [FromForm] string category,
        [FromForm] string? contractId,
        [FromForm] string? occupantId,
        [FromForm] string? tenantId,
        [FromForm] string? propertyId,
        [FromForm] string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            // Save file to disk
            var originalBaseName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var fileName = SanitizeFileName($"{originalBaseName}_{Guid.NewGuid():N}{extension}");
            var documentsPath = Path.Combine(_environment.ContentRootPath, "Documents", _orgContext.OrganizationId!.Value.ToString());
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
                ContractId = string.IsNullOrEmpty(contractId) ? null : Guid.Parse(contractId),
                OccupantId = !string.IsNullOrEmpty(occupantId)
                    ? Guid.Parse(occupantId)
                    : (string.IsNullOrEmpty(tenantId) ? null : Guid.Parse(tenantId)),
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
    [Authorize(Policy = Permissions.DocumentsRead)]
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

            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(fileBytes, contentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error downloading document", error = ex.Message });
        }
    }

    [HttpGet("view/{documentId}")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    public async Task<IActionResult> ViewDocument(string documentId)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(Guid.Parse(documentId));
            if (document == null)
                return NotFound(new { message = "Document not found" });

            if (!System.IO.File.Exists(document.FilePath))
                return NotFound(new { message = "File not found on disk" });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);

            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{document.FileName}\"";
            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error viewing document", error = ex.Message });
        }
    }

    [HttpDelete("{documentId}/dissociate")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
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

    [HttpGet("occupant/{occupantId}/export-zip")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    public async Task<IActionResult> ExportDocumentsZip(string occupantId)
    {
        try
        {
            var query = new ExportDocumentsZipQuery { OccupantId = occupantId };
            var zipBytes = await _mediator.Send(query);

            if (zipBytes.Length == 0)
                return NotFound(new { message = "No documents found for this occupant" });

            var fileName = $"Documents_Locataire_{DateTime.UtcNow:yyyy-MM-dd}.zip";
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting documents ZIP for occupant {OccupantId}", occupantId);
            return StatusCode(500, new { message = "Error exporting documents", error = ex.Message });
        }
    }

    [HttpPost("generate-quittance")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    public async Task<IActionResult> GenerateQuittance([FromBody] GenerateQuittanceCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            
            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            var fileName = $"Quittance_{command.Month.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(result.Data!, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quittance");
            return StatusCode(500, new { message = "Error generating quittance", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Marquer un document comme signé
    /// PUT /api/documents/{id}/mark-signed
    /// </summary>
    [HttpPut("{id:guid}/mark-signed")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    public async Task<IActionResult> MarkDocumentAsSigned(
        Guid id, 
        [FromBody] MarkDocumentAsSignedRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id, cancellationToken);
            if (document == null)
                return NotFound(new { message = "Document not found" });

            document.MarkAsSigned(request?.SignedDate, request?.SignedBy);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Document {DocumentId} marked as signed by {SignedBy}", id, request?.SignedBy ?? "Unknown");

            return Ok(new 
            { 
                message = "Document marked as signed successfully", 
                id = document.Id,
                status = document.Status.ToString(),
                signedDate = document.SignedDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking document {DocumentId} as signed", id);
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Récupérer le statut des documents d'un contrat
    /// GET /api/documents/contract/{contractId}/status
    /// </summary>
    [HttpGet("contract/{contractId:guid}/status")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    public async Task<IActionResult> GetContractDocumentStatus(
        Guid contractId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId, cancellationToken);
            if (contract == null)
                return NotFound(new { message = "Contract not found" });

            var documents = await _unitOfWork.Documents.GetByContractIdAsync(contractId, cancellationToken);

            return Ok(new
            {
                contractId,
                contractStatus = contract.Status.ToString(),
                requiredDocuments = contract.RequiredDocuments.Select(r => new
                {
                    type = r.Type.ToString(),
                    isRequired = r.IsRequired,
                    isProvided = r.IsProvided,
                    isSigned = r.IsSigned,
                    documentInfo = documents.FirstOrDefault(d => d.Type == r.Type) != null ? new
                    {
                        id = documents.First(d => d.Type == r.Type).Id,
                        fileName = documents.First(d => d.Type == r.Type).FileName,
                        status = documents.First(d => d.Type == r.Type).Status.ToString(),
                        signedDate = documents.First(d => d.Type == r.Type).SignedDate,
                        createdAt = documents.First(d => d.Type == r.Type).CreatedAt
                    } : null
                }),
                allRequiredSigned = contract.AreAllRequiredDocumentsSigned()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract document status for {ContractId}", contractId);
            return StatusCode(500, new { message = "Error getting contract document status", error = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer les informations complètes d'un contrat pour affichage dans un viewer (UI)
    /// GET /api/documents/contract/{contractId}/viewer
    /// </summary>
    [HttpGet("contract/{contractId:guid}/viewer")]
    public async Task<IActionResult> GetContractViewer(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId, cancellationToken);
            if (contract == null)
                return NotFound(new { message = "Contract not found" });

            var effectiveStateResult = await _effectiveContractStateResolver.ResolveAsync(contractId, DateTime.UtcNow, cancellationToken);
            var effective = effectiveStateResult.IsSuccess ? effectiveStateResult.Data : null;

            var nextSigned = await _unitOfWork.Addendums.Query()
                .AsNoTracking()
                .Where(a => a.ContractId == contractId
                            && a.SignatureStatus == AddendumSignatureStatus.Signed
                            && a.EffectiveDate > DateTime.UtcNow)
                .OrderBy(a => a.EffectiveDate)
                .ThenBy(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    type = a.Type.ToString(),
                    effectiveDate = a.EffectiveDate,
                    newRent = a.NewRent,
                    newCharges = a.NewCharges,
                    newEndDate = a.NewEndDate,
                    newRoomId = a.NewRoomId
                })
                .FirstOrDefaultAsync(cancellationToken);

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken);
            if (occupant == null)
                return NotFound(new { message = "Occupant not found" });

            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            if (property == null)
                return NotFound(new { message = "Property not found" });

            var documents = await _unitOfWork.Documents.GetByContractIdAsync(contractId, cancellationToken);

            var documentsStatus = new
            {
                contractId,
                contractStatus = contract.Status.ToString(),
                requiredDocuments = contract.RequiredDocuments.Select(r => new
                {
                    type = r.Type.ToString(),
                    isRequired = r.IsRequired,
                    isProvided = r.IsProvided,
                    isSigned = r.IsSigned,
                    documentInfo = documents.FirstOrDefault(d => d.Type == r.Type) != null ? new
                    {
                        id = documents.First(d => d.Type == r.Type).Id,
                        fileName = documents.First(d => d.Type == r.Type).FileName,
                        status = documents.First(d => d.Type == r.Type).Status.ToString(),
                        signedDate = documents.First(d => d.Type == r.Type).SignedDate,
                        createdAt = documents.First(d => d.Type == r.Type).CreatedAt
                    } : null
                }),
                allRequiredSigned = contract.AreAllRequiredDocumentsSigned()
            };

            return Ok(new
            {
                contract = new
                {
                    id = contract.Id,
                    code = contract.Code,
                    status = contract.Status.ToString(),
                    type = contract.Type.ToString(),
                    startDate = contract.StartDate,
                    endDate = contract.EndDate,
                    rent = contract.Rent,
                    charges = contract.Charges,
                    deposit = contract.Deposit,
                    roomId = contract.RoomId,
                    notes = contract.Notes,
                    terminationDate = contract.TerminationDate,
                    terminationReason = contract.TerminationReason,
                    createdAt = contract.CreatedAt
                },
                effective,
                nextSignedChange = nextSigned,
                occupant = new
                {
                    id = occupant.Id,
                    code = occupant.Code,
                    fullName = occupant.FullName,
                    email = occupant.Email,
                    phone = occupant.Phone
                },
                property = new
                {
                    id = property.Id,
                    code = property.Code,
                    name = property.Name,
                    address = property.Address,
                    city = property.City,
                    postalCode = property.PostalCode
                },
                documentsStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract viewer for {ContractId}", contractId);
            return StatusCode(500, new { message = "Error getting contract viewer", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Envoyer un document pour signature électronique
    /// POST /api/documents/{id}/send-for-signature
    /// </summary>
    [HttpPost("{id:guid}/send-for-signature")]
    public async Task<IActionResult> SendDocumentForElectronicSignature(
        Guid id, 
        [FromBody] SendDocumentForSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id, cancellationToken);
            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Vérifier que le document est un PDF
            if (!document.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only PDF documents can be sent for electronic signature" });

            // Si le document est lié à un contrat, vérifier son statut
            var contractId = await _readDb.ContractDocumentLinks
                .AsNoTracking()
                .Where(x => x.DocumentId == document.Id)
                .Select(x => (Guid?)x.ContractId)
                .FirstOrDefaultAsync(cancellationToken);

            if (contractId.HasValue)
            {
                // ✅ Autoriser l'envoi des avenants même si le contrat est Active/Signed.
                // La restriction ne s'applique qu'aux documents de bail (contrat).
                if (document.Type != Domain.Aggregates.DocumentAggregate.DocumentType.Avenant)
                {
                    var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId.Value, cancellationToken);
                    if (contract != null && contract.Status != Domain.Aggregates.ContractAggregate.ContractStatus.Draft)
                    {
                        return BadRequest(new { message = "Only draft contracts can be sent for signature" });
                    }
                }
            }

            // TODO: Implémenter l'intégration avec un service de signature électronique
            // Services recommandés:
            // - DocuSign API: https://developers.docusign.com/
            // - HelloSign API (Dropbox Sign): https://www.hellosign.com/api
            // - Adobe Sign API: https://www.adobe.io/apis/documentcloud/sign.html
            // - Yousign API (France): https://yousign.com/fr-fr/api
            
            // Processus type:
            // 1. Lire le fichier PDF depuis le disque
            // 2. Créer une enveloppe de signature
            // 3. Ajouter le document à l'enveloppe
            // 4. Ajouter les signataires (destinataires)
            // 5. Configurer les champs de signature
            // 6. Envoyer l'enveloppe
            // 7. Sauvegarder l'ID d'enveloppe pour tracking
            // 8. Mettre à jour le statut du document/contrat

            _logger.LogInformation(
                "Document {DocumentId} ({FileName}) sent for electronic signature to {Recipients}",
                id, 
                document.FileName,
                string.Join(", ", request.Recipients.Select(r => r.Email)));

            return Ok(new
            {
                message = "Document sent for electronic signature successfully",
                documentId = id,
                fileName = document.FileName,
                recipients = request.Recipients.Count,
                status = "sent",
                // envelopeId = "xxx-xxx-xxx" // ID retourné par le service de signature
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document {DocumentId} for signature", id);
            return StatusCode(500, new { message = "Error sending document for signature", error = ex.Message });
        }
    }
    
    /// <summary>
    /// Récupérer un document par son ID
    /// GET /api/documents/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id, cancellationToken);
            if (document == null)
                return NotFound(new { message = "Document not found" });

            return Ok(new
            {
                document.Id,
                document.Code,
                document.FileName,
                Type = document.Type.ToString(),
                Category = document.Category.ToString(),
                Status = document.Status.ToString(),
                document.FileSizeBytes,
                ContractId = await _readDb.ContractDocumentLinks
                    .AsNoTracking()
                    .Where(x => x.DocumentId == document.Id)
                    .Select(x => (Guid?)x.ContractId)
                    .FirstOrDefaultAsync(cancellationToken),
                document.AssociatedOccupantId,
                document.PropertyId,
                document.Description,
                document.SignedDate,
                document.SignedBy,
                document.CreatedAt,
                document.IsArchived
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", id);
            return StatusCode(500, new { message = "Error getting document", error = ex.Message });
        }
    }
}

public record MarkDocumentAsSignedRequest(
    DateTime? SignedDate = null,
    string? SignedBy = null
);

public record SendDocumentForSignatureRequest(
    List<SignatureRecipient> Recipients,
    string? Message = null,
    int? ExpirationDays = 30
);

public record SignatureRecipient(
    string Email,
    string Name,
    int SigningOrder = 1
);
