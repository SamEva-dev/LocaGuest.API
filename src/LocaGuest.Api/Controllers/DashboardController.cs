using LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;
using LocaGuest.Application.Features.Dashboard.Queries.GetRecentActivities;
using LocaGuest.Application.Features.Dashboard.Queries.GetDeadlines;
using LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;
using LocaGuest.Application.Features.Dashboard.Queries.GetRevenueChart;
using LocaGuest.Application.Features.Dashboard.Queries.GetAvailableYears;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;
    private readonly IMediator _mediator;

    public DashboardController(ILogger<DashboardController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? month, [FromQuery] int? year)
    {
        var query = new GetDashboardSummaryQuery 
        { 
            Month = month, 
            Year = year 
        };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] int limit = 20)
    {
        var query = new GetRecentActivitiesQuery { Limit = limit };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("deadlines")]
    public async Task<IActionResult> GetDeadlines()
    {
        var query = new GetDeadlinesQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("charts/occupancy")]
    public async Task<IActionResult> GetOccupancyChart([FromQuery] int year)
    {
        if (year == 0)
            year = DateTime.UtcNow.Year;

        var query = new GetOccupancyChartQuery(year);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("charts/revenue")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int year)
    {
        if (year == 0)
            year = DateTime.UtcNow.Year;

        var query = new GetRevenueChartQuery(year);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("available-years")]
    public async Task<IActionResult> GetAvailableYears()
    {
        var query = new GetAvailableYearsQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
