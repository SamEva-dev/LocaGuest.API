using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Features.Properties.Queries.GetProperty;
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
    public IActionResult GetPropertyPayments(string id)
    {
        // TODO: Implement GetPropertyPaymentsQuery
        // For now, return empty list
        return Ok(new object[0]);
    }

    [HttpGet("{id}/contracts")]
    public IActionResult GetPropertyContracts(string id)
    {
        // TODO: Implement GetPropertyContractsQuery
        // For now, return empty list
        return Ok(new object[0]);
    }

    [HttpGet("{id}/financial-summary")]
    public IActionResult GetFinancialSummary(string id)
    {
        // TODO: Implement GetFinancialSummaryQuery
        // For now, return empty object
        return Ok(new { });
    }
}
