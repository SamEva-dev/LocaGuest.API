using LocaGuest.Application.Features.Settings.Queries.GetUserSettings;
using LocaGuest.Application.Features.Settings.Commands.UpdateUserSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IMediator mediator, ILogger<SettingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
    {
        var query = new GetUserSettingsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateUserSettingsCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
}
