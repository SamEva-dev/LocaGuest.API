using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Commands.UpdateProperty;
using LocaGuest.Application.Features.Properties.Commands.UpdatePropertyStatus;
using LocaGuest.Application.Features.Properties.Commands.DissociateTenant;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Features.Properties.Queries.GetProperty;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyPayments;
using LocaGuest.Application.Features.Properties.Queries.GetFinancialSummary;
using LocaGuest.Application.Features.Properties.Queries.GetAssociatedTenants;
using LocaGuest.Application.Features.Tenants.Queries.GetAvailableTenants;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PropertiesController> _logger;
    private readonly LocaGuestDbContext _context;

    public PropertiesController(
        IMediator mediator, 
        ILogger<PropertiesController> logger,
        LocaGuestDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties([FromQuery] GetPropertiesQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetProperties), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProperty(string id)
    {
        var query = new GetPropertyQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProperty(string id, [FromBody] UpdatePropertyCommand command)
    {
        if (!Guid.TryParse(id, out var propertyId) || command.Id != propertyId)
            return BadRequest(new { message = "Property ID mismatch" });

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdatePropertyStatus(string id, [FromBody] UpdatePropertyStatusCommand command)
    {
        if (!Guid.TryParse(id, out var propertyId) || command.PropertyId != propertyId)
            return BadRequest(new { message = "Property ID mismatch" });

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { success = result.Data });
    }

    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPropertyPayments(string id)
    {
        var query = new GetPropertyPaymentsQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id}/contracts")]
    public async Task<IActionResult> GetPropertyContracts(string id)
    {
        var query = new GetPropertyContractsQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id}/associated-tenants")]
    public async Task<IActionResult> GetAssociatedTenants(string id)
    {
        var query = new GetAssociatedTenantsQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id}/financial-summary")]
    public async Task<IActionResult> GetFinancialSummary(string id)
    {
        var query = new GetFinancialSummaryQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id}/available-tenants")]
    public async Task<IActionResult> GetAvailableTenants(string id)
    {
        var query = new GetAvailableTenantsQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost("{id}/assign-tenant")]
    public async Task<IActionResult> AssignTenant(string id, [FromBody] CreateContractCommand command)
    {
        if (id != command.PropertyId.ToString())
            return BadRequest(new { message = "Property ID mismatch" });

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpDelete("{propertyId}/dissociate-tenant/{tenantId}")]
    public async Task<IActionResult> DissociateTenant(string propertyId, string tenantId)
    {
        var command = new DissociateTenantCommand
        {
            PropertyId = propertyId,
            TenantId = tenantId
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return NoContent();
    }
    
    /// <summary>
    /// Supprimer un bien (uniquement si aucun contrat actif ou signé)
    /// DELETE /api/properties/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProperty(string id)
    {
        if (!Guid.TryParse(id, out var propertyGuid))
            return BadRequest(new { message = "Invalid property ID format" });

        var property = await _context.Properties.FindAsync(propertyGuid);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        // ✅ VALIDATION: Vérifier qu'il n'y a pas de contrats actifs ou signés
        var activeContracts = await _context.Contracts
            .Where(c => c.PropertyId == propertyGuid &&
                       (c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active ||
                        c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Signed))
            .ToListAsync();

        if (activeContracts.Any())
        {
            return BadRequest(new { 
                message = $"Impossible de supprimer le bien. Il possède {activeContracts.Count} contrat(s) actif(s) ou signé(s). Veuillez d'abord résilier ces contrats.",
                activeContractsCount = activeContracts.Count
            });
        }

        try
        {
            // ✅ CASCADE: Récupérer tous les contrats (Draft, Cancelled, Expired, Terminated)
            var allContracts = await _context.Contracts
                .Include(c => c.Payments)
                .Where(c => c.PropertyId == propertyGuid)
                .ToListAsync();

            int deletedPayments = 0;
            int deletedDocuments = 0;

            foreach (var contract in allContracts)
            {
                // Supprimer les paiements
                if (contract.Payments.Any())
                {
                    _context.Payments.RemoveRange(contract.Payments);
                    deletedPayments += contract.Payments.Count;
                }

                // Supprimer les documents du contrat
                var contractDocuments = await _context.Documents
                    .Where(d => d.ContractId == contract.Id)
                    .ToListAsync();
                    
                if (contractDocuments.Any())
                {
                    _context.Documents.RemoveRange(contractDocuments);
                    deletedDocuments += contractDocuments.Count;
                }
            }

            // Supprimer les contrats
            _context.Contracts.RemoveRange(allContracts);

            // ✅ CASCADE: Supprimer les documents directement liés au bien
            var propertyDocuments = await _context.Documents
                .Where(d => d.PropertyId == propertyGuid)
                .ToListAsync();
                
            if (propertyDocuments.Any())
            {
                _context.Documents.RemoveRange(propertyDocuments);
                deletedDocuments += propertyDocuments.Count;
            }

            // ✅ CASCADE: Dissocier les locataires associés au bien
            var associatedTenants = await _context.Tenants
                .Where(t => t.PropertyId == propertyGuid)
                .ToListAsync();
                
            foreach (var tenant in associatedTenants)
            {
                tenant.DissociateFromProperty();
            }

            // Supprimer le bien
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Bien {PropertyCode} supprimé avec succès. Contrats: {ContractCount}, Paiements: {PaymentCount}, Documents: {DocumentCount}, Locataires dissociés: {TenantCount}",
                property.Code, allContracts.Count, deletedPayments, deletedDocuments, associatedTenants.Count);
                
            return Ok(new { 
                message = "Bien supprimé avec succès", 
                id = property.Id,
                deletedContracts = allContracts.Count,
                deletedPayments,
                deletedDocuments,
                dissociatedTenants = associatedTenants.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la suppression du bien {PropertyId}", propertyGuid);
            return StatusCode(500, new { message = "Erreur lors de la suppression du bien", error = ex.Message });
        }
    }
}
