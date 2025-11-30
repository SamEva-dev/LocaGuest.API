using LocaGuest.Application.Features.Inventories.Commands.CreateInventoryEntry;
using LocaGuest.Application.Features.Inventories.Commands.CreateInventoryExit;
using LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryEntry;
using LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryExit;
using LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;
using LocaGuest.Application.Features.Inventories.Commands.SignInventory;
using LocaGuest.Application.Features.Inventories.Commands.FinalizeInventoryEntry;
using LocaGuest.Application.Features.Inventories.Queries.GetInventoryEntry;
using LocaGuest.Application.Features.Inventories.Queries.GetInventoryExit;
using LocaGuest.Application.Features.Inventories.Queries.GetInventoryByContract;
using LocaGuest.Application.Features.Inventories.Queries.GenerateInventoryPdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

/// <summary>
/// Contrôleur pour la gestion des états des lieux (inventaires)
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoriesController> _logger;

    public InventoriesController(IMediator mediator, ILogger<InventoriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Créer un état des lieux d'entrée
    /// </summary>
    /// <param name="command">Données de l'EDL d'entrée</param>
    /// <returns>L'EDL créé</returns>
    [HttpPost("entry")]
    [ProducesResponseType(typeof(Application.DTOs.Inventories.InventoryEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEntry([FromBody] CreateInventoryEntryCommand command)
    {
        _logger.LogInformation("Creating inventory entry for contract {ContractId}", command.ContractId);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create inventory entry: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory entry created successfully: {InventoryId}", result.Data!.Id);
        return Ok(result.Data);
    }

    /// <summary>
    /// Créer un état des lieux de sortie
    /// </summary>
    /// <param name="command">Données de l'EDL de sortie</param>
    /// <returns>L'EDL créé avec calcul de déduction</returns>
    [HttpPost("exit")]
    [ProducesResponseType(typeof(Application.DTOs.Inventories.InventoryExitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExit([FromBody] CreateInventoryExitCommand command)
    {
        _logger.LogInformation("Creating inventory exit for contract {ContractId}", command.ContractId);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create inventory exit: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory exit created successfully: {InventoryId}, Deduction: {Amount}€", 
            result.Data!.Id, result.Data.TotalDeductionAmount);
        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer un EDL d'entrée par ID
    /// </summary>
    /// <param name="id">ID de l'EDL d'entrée</param>
    /// <returns>L'EDL d'entrée</returns>
    [HttpGet("entry/{id:guid}")]
    [ProducesResponseType(typeof(Application.DTOs.Inventories.InventoryEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntry(Guid id)
    {
        var result = await _mediator.Send(new GetInventoryEntryQuery(id));
        
        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer un EDL de sortie par ID
    /// </summary>
    /// <param name="id">ID de l'EDL de sortie</param>
    /// <returns>L'EDL de sortie</returns>
    [HttpGet("exit/{id:guid}")]
    [ProducesResponseType(typeof(Application.DTOs.Inventories.InventoryExitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExit(Guid id)
    {
        var result = await _mediator.Send(new GetInventoryExitQuery(id));
        
        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer les EDL (entrée + sortie) d'un contrat
    /// </summary>
    /// <param name="contractId">ID du contrat</param>
    /// <returns>EDL entrée et sortie s'ils existent</returns>
    [HttpGet("contract/{contractId:guid}")]
    [ProducesResponseType(typeof(ContractInventoriesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByContract(Guid contractId)
    {
        var result = await _mediator.Send(new GetInventoryByContractQuery(contractId));
        
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Finaliser un état des lieux d'entrée (signer et verrouiller)
    /// </summary>
    /// <param name="id">ID de l'EDL</param>
    [HttpPut("entry/{id:guid}/finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinalizeEntry(Guid id)
    {
        _logger.LogInformation("Finalizing inventory entry {InventoryEntryId}", id);
        
        var command = new FinalizeInventoryEntryCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to finalize inventory entry: {Error}", result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage });
        }
        
        _logger.LogInformation("Inventory entry finalized successfully");
        return Ok(new { message = "Inventory entry finalized successfully" });
    }

    /// <summary>
    /// Supprimer un EDL d'entrée
    /// </summary>
    /// <param name="id">ID de l'EDL d'entrée</param>
    /// <returns>Confirmation de suppression</returns>
    [HttpDelete("entry/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        _logger.LogInformation("Deleting inventory entry {InventoryId}", id);
        
        var result = await _mediator.Send(new DeleteInventoryEntryCommand(id));
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete inventory entry: {Error}", result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory entry deleted successfully: {InventoryId}", id);
        return Ok(new { message = "Inventory entry deleted successfully" });
    }

    /// <summary>
    /// Supprimer un EDL de sortie
    /// </summary>
    /// <param name="id">ID de l'EDL de sortie</param>
    /// <returns>Confirmation de suppression</returns>
    [HttpDelete("exit/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExit(Guid id)
    {
        _logger.LogInformation("Deleting inventory exit {InventoryId}", id);
        
        var result = await _mediator.Send(new DeleteInventoryExitCommand(id));
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete inventory exit: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory exit deleted successfully: {InventoryId}", id);
        return Ok(new { message = "Inventory exit deleted successfully" });
    }

    /// <summary>
    /// Générer le PDF d'un EDL
    /// </summary>
    /// <param name="id">ID de l'EDL</param>
    /// <param name="type">Type d'EDL: entry ou exit</param>
    /// <returns>PDF de l'EDL</returns>
    [HttpGet("pdf/{type}/{id:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePdf(Guid id, string type)
    {
        _logger.LogInformation("Generating PDF for inventory {Type} {InventoryId}", type, id);
        
        var query = new GenerateInventoryPdfQuery
        {
            InventoryId = id,
            InventoryType = type == "entry" ? "Entry" : "Exit"
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess || result.Data == null)
        {
            _logger.LogWarning("Failed to generate PDF: {Error}", result.ErrorMessage);
            return NotFound(new { error = result.ErrorMessage });
        }

        var fileName = type == "entry" 
            ? $"EDL_Entree_{DateTime.Now:yyyyMMdd}.pdf"
            : $"EDL_Sortie_{DateTime.Now:yyyyMMdd}.pdf";

        return File(result.Data, "application/pdf", fileName);
    }

    /// <summary>
    /// Envoyer l'EDL par email au locataire
    /// </summary>
    /// <param name="command">Détails de l'envoi email</param>
    /// <returns>Confirmation d'envoi</returns>
    [HttpPost("send-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmail([FromBody] SendInventoryEmailCommand command)
    {
        _logger.LogInformation("Sending inventory email for {Type} {InventoryId} to {Email}", 
            command.InventoryType, command.InventoryId, command.RecipientEmail);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to send email: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory email sent successfully");
        return Ok(new { message = "Email sent successfully" });
    }

    /// <summary>
    /// Signer électroniquement un EDL
    /// </summary>
    /// <param name="command">Données de signature</param>
    /// <returns>Confirmation de signature</returns>
    [HttpPost("sign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignInventory([FromBody] SignInventoryCommand command)
    {
        _logger.LogInformation("Signing inventory {Type} {InventoryId} by {SignerRole}", 
            command.InventoryType, command.InventoryId, command.SignerRole);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to sign inventory: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Inventory signed successfully");
        return Ok(new { message = "Inventory signed successfully" });
    }
}
