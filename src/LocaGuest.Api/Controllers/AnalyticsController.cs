using LocaGuest.Application.Features.Analytics.Queries.GetProfitabilityStats;
using LocaGuest.Application.Features.Analytics.Queries.GetRevenueEvolution;
using LocaGuest.Application.Features.Analytics.Queries.GetPropertyPerformance;
using LocaGuest.Application.Features.Analytics.Queries.GetAvailableYears;
using LocaGuest.Application.Features.Analytics.Queries.GetOccupancyTrend;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IMediator mediator, ILogger<AnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("profitability-stats")]
    public async Task<IActionResult> GetProfitabilityStats()
    {
        var query = new GetProfitabilityStatsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("revenue-evolution")]
    public async Task<IActionResult> GetRevenueEvolution([FromQuery] int months = 6, [FromQuery] int? year = null)
    {
        var query = new GetRevenueEvolutionQuery { Months = months, Year = year };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("property-performance")]
    public async Task<IActionResult> GetPropertyPerformance()
    {
        var query = new GetPropertyPerformanceQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("available-years")]
    public async Task<IActionResult> GetAvailableYears()
    {
        var query = new GetAvailableYearsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("occupancy-trend")]
    public async Task<IActionResult> GetOccupancyTrend([FromQuery] int days = 30)
    {
        var query = new GetOccupancyTrendQuery { Days = days };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
}
