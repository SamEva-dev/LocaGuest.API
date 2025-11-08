using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
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
}
