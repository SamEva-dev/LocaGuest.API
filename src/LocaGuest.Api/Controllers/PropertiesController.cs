using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Commands.DeleteProperty;
using LocaGuest.Application.Features.Properties.Commands.UpdateProperty;
using LocaGuest.Application.Features.Properties.Commands.UpdatePropertyStatus;
using LocaGuest.Application.Features.Properties.Commands.DissociateTenant;
using LocaGuest.Application.Features.Properties.Commands.SetCoverImage;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Features.Properties.Queries.GetProperty;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyPayments;
using LocaGuest.Application.Features.Properties.Queries.GetFinancialSummary;
using LocaGuest.Application.Features.Properties.Queries.GetAssociatedOccupants;
using LocaGuest.Application.Features.Occupants.Queries.GetAvailableOccupants;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocaGuest.Api.Authorization;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(
        IMediator mediator, 
        ILogger<PropertiesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties([FromQuery] GetPropertiesQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost("{propertyId}/images/{imageId}/set-cover")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> SetCoverImage(string propertyId, string imageId)
    {
        if (!Guid.TryParse(propertyId, out var propertyGuid))
            return BadRequest(new { message = "Invalid property ID format" });

        if (!Guid.TryParse(imageId, out var imageGuid))
            return BadRequest(new { message = "Invalid image ID format" });

        var result = await _mediator.Send(new SetCoverImageCommand
        {
            PropertyId = propertyGuid,
            ImageId = imageGuid
        });

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PropertiesWrite)]
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
    [Authorize(Policy = Permissions.PropertiesWrite)]
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
    [Authorize(Policy = Permissions.PropertiesWrite)]
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

    [HttpGet("{id}/associated-occupants")]
    public async Task<IActionResult> GetAssociatedOccupants(string id)
    {
        var query = new GetAssociatedOccupantsQuery { PropertyId = id };
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

    [HttpGet("{id}/available-occupants")]
    public async Task<IActionResult> GetAvailableOccupants(string id)
    {
        var query = new GetAvailableOccupantsQuery { PropertyId = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost("{id}/assign-occupant")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> AssignOccupant(string id, [FromBody] CreateContractCommand command)
    {
        if (id != command.PropertyId.ToString())
            return BadRequest(new { message = "Property ID mismatch" });

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpDelete("{propertyId}/dissociate-tenant/{OccupantId}")]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> DissociateTenant(string propertyId, string OccupantId)
    {
        var command = new DissociateTenantCommand
        {
            PropertyId = propertyId,
            OccupantId = OccupantId
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
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public async Task<IActionResult> DeleteProperty(string id)
    {
        if (!Guid.TryParse(id, out var propertyGuid))
            return BadRequest(new { message = "Invalid property ID format" });

        var result = await _mediator.Send(new DeletePropertyCommand { PropertyId = propertyGuid });

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorMessage, "Property not found", StringComparison.Ordinal))
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Bien supprimé avec succès",
            id = result.Data!.Id,
            deletedContracts = result.Data.DeletedContracts,
            deletedPayments = result.Data.DeletedPayments,
            deletedDocuments = result.Data.DeletedDocuments,
            dissociatedTenants = result.Data.DissociatedTenants
        });
    }
}
