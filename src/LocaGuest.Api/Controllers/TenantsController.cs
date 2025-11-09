using LocaGuest.Application.Features.Tenants.Commands.CreateTenant;
using LocaGuest.Application.Features.Tenants.Queries.GetTenants;
using LocaGuest.Application.Features.Tenants.Queries.GetTenant;
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

    public TenantsController(IMediator mediator, ILogger<TenantsController> logger)
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
    public IActionResult GetTenantContracts(string id)
    {
        // TODO: Implement GetTenantContractsQuery
        // For now, return empty list
        return Ok(new object[0]);
    }

    [HttpGet("{id}/payment-stats")]
    public IActionResult GetPaymentStats(string id)
    {
        // TODO: Implement GetPaymentStatsQuery
        // For now, return empty object
        return Ok(new { totalPaid = 0, totalPayments = 0, latePayments = 0, onTimeRate = 0 });
    }
}
