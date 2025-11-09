using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Features.Properties.Queries.GetProperty;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;
using LocaGuest.Application.Features.Properties.Queries.GetPropertyPayments;
using LocaGuest.Application.Features.Properties.Queries.GetFinancialSummary;
using LocaGuest.Application.Features.Tenants.Queries.GetAvailableTenants;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(IMediator mediator, ILogger<PropertiesController> logger)
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
}
