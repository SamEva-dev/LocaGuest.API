using LocaGuest.Application.Features.PublicStats.Queries.GetPublicStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicStatsController : ControllerBase
{
    private readonly ILogger<PublicStatsController> _logger;
    private readonly IMediator _mediator;

    public PublicStatsController(ILogger<PublicStatsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Get public statistics for landing page (no authentication required)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var query = new GetPublicStatsQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
