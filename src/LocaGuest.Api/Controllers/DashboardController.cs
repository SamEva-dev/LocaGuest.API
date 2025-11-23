using LocaGuest.Application.Features.Dashboard.Queries.GetDashboardSummary;
using LocaGuest.Application.Features.Dashboard.Queries.GetRecentActivities;
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
    public async Task<IActionResult> GetSummary()
    {
        var query = new GetDashboardSummaryQuery();
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
    public IActionResult GetDeadlines()
    {
        // TODO: Implement real logic
        var deadlines = new
        {
            lateRent = new[] { new { propertyName = "T3 Centre Ville", amount = 850m, dayslate = 5 } },
            nextDue = new[] { new { propertyName = "Studio Quartier Gare", amount = 450m, daysUntil = 3 } },
            renewals = new[] { new { propertyName = "T2 Quartier Nord", daysUntil = 45 } }
        };

        return Ok(deadlines);
    }

    [HttpGet("charts/occupancy")]
    public IActionResult GetOccupancyChart([FromQuery] int year = 2025)
    {
        // TODO: Implement real logic
        var series = Enumerable.Range(1, 12).Select(month => new
        {
            month,
            rate = 0.85m + (month % 3) * 0.05m
        });

        return Ok(series);
    }

    [HttpGet("charts/revenue")]
    public IActionResult GetRevenueChart([FromQuery] int year = 2025)
    {
        // TODO: Implement real logic
        var series = Enumerable.Range(1, 12).Select(month => new
        {
            month,
            revenue = 12000m + (month * 500)
        });

        return Ok(series);
    }
}
