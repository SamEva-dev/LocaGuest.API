using LocaGuest.Application.Features.Tenants.Commands.CreateTenant;
using LocaGuest.Application.Features.Tenants.Commands.ChangeTenantStatus;
using LocaGuest.Application.Features.Tenants.Commands.DeleteTenant;
using LocaGuest.Application.Features.Tenants.Queries.GetTenants;
using LocaGuest.Application.Features.Tenants.Queries.GetTenant;
using LocaGuest.Application.Features.Tenants.Queries.GetTenantContracts;
using LocaGuest.Application.Features.Tenants.Queries.GetTenantPaymentStats;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        IMediator mediator, 
        ILogger<TenantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants([FromQuery] GetTenantsQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetTenants), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(string id)
    {
        var query = new GetTenantQuery { Id = id };
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
    public IActionResult GetTenantPayments(string id)
    {
        // TODO: Implement GetTenantPaymentsQuery
        // For now, return empty list
        return Ok(new object[0]);
    }

    [HttpGet("{id}/contracts")]
    public async Task<IActionResult> GetTenantContracts(string id)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        var query = new GetTenantContractsQuery(tenantGuid);
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
    public async Task<IActionResult> GetPaymentStats(string id)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        var query = new GetTenantPaymentStatsQuery(tenantGuid);
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
    /// Supprimer un locataire (uniquement si aucun contrat actif ou signé)
    /// DELETE /api/tenants/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTenant(string id)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        var command = new DeleteTenantCommand { TenantId = tenantGuid };
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Locataire supprimé avec succès",
            id = result.Data!.Id,
            deletedContracts = result.Data.DeletedContracts,
            deletedPayments = result.Data.DeletedPayments,
            deletedDocuments = result.Data.DeletedDocuments
        });
    }

    public record ChangeStatusRequest(string Status);

    [HttpPost("{id}/change-status")]
    public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeStatusRequest request)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !Enum.TryParse<TenantStatus>(request.Status, true, out var statusEnum))
        {
            return BadRequest(new { message = "Invalid status" });
        }

        var command = new ChangeTenantStatusCommand
        {
            TenantId = tenantGuid,
            Status = statusEnum
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { success = true });
    }
}
