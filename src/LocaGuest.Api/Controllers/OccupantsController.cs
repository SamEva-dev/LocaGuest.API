using LocaGuest.Application.Features.Occupants.Commands.CreateOccupant;
using LocaGuest.Application.Features.Occupants.Commands.ChangeOccupantStatus;
using LocaGuest.Application.Features.Occupants.Commands.DeleteOccupant;
using LocaGuest.Application.Features.Occupants.Queries.GetOccupants;
using LocaGuest.Application.Features.Occupants.Queries.GetOccupant;
using LocaGuest.Application.Features.Occupants.Queries.GetOccupantContracts;
using LocaGuest.Application.Features.Occupants.Queries.GetOccupantPaymentStats;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsByTenant;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OccupantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OccupantsController> _logger;

    public OccupantsController(
        IMediator mediator, 
        ILogger<OccupantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsRead)]
    public async Task<IActionResult> GetOccupants([FromQuery] GetOccupantsQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsWrite)]
    public async Task<IActionResult> CreateOccupant([FromBody] CreateOccupantCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetOccupants), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsRead)]
    public async Task<IActionResult> GetOccupant(string id)
    {
        var query = new GetOccupantQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}/payments")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsRead)]
    public async Task<IActionResult> GetOccupantPayments(string id, CancellationToken cancellationToken)
    {
        var query = new GetPaymentsByTenantQuery { OccupantId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}/contracts")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsRead)]
    public async Task<IActionResult> GetOccupantContracts(string id)
    {
        if (!Guid.TryParse(id, out var occupantGuid))
            return BadRequest(new { message = "Invalid occupant ID format" });

        var query = new GetOccupantContractsQuery(occupantGuid);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}/payment-stats")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsRead)]
    public async Task<IActionResult> GetPaymentStats(string id)
    {
        if (!Guid.TryParse(id, out var occupantGuid))
            return BadRequest(new { message = "Invalid occupant ID format" });

        var query = new GetOccupantPaymentStatsQuery(occupantGuid);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }
    
    /// <summary>
    /// Supprimer un occupant (uniquement si aucun contrat actif ou signé)
    /// DELETE /api/occupants/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsDelete)]
    public async Task<IActionResult> DeleteOccupant(string id)
    {
        if (!Guid.TryParse(id, out var occupantGuid))
            return BadRequest(new { message = "Invalid occupant ID format" });

        var command = new DeleteOccupantCommand { OccupantId = occupantGuid };
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Occupant supprimé avec succès",
            id = result.Data!.Id,
            deletedContracts = result.Data.DeletedContracts,
            deletedPayments = result.Data.DeletedPayments,
            deletedDocuments = result.Data.DeletedDocuments
        });
    }

    public record ChangeStatusRequest(string Status);

    [HttpPost("{id}/change-status")]
    [Authorize(Policy = LocaGuest.Api.Authorization.Permissions.TenantsWrite)]
    public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeStatusRequest request)
    {
        if (!Guid.TryParse(id, out var occupantGuid))
            return BadRequest(new { message = "Invalid occupant ID format" });

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !Enum.TryParse<OccupantStatus>(request.Status, true, out var statusEnum))
        {
            return BadRequest(new { message = "Invalid status" });
        }

        var command = new ChangeOccupantStatusCommand
        {
            OccupantId = occupantGuid,
            Status = statusEnum
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { success = true });
    }
}
