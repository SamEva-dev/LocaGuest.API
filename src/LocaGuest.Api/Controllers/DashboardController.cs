using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        // TODO: Implement real logic
        var summary = new
        {
            propertiesCount = 12,
            activeTenants = 35,
            occupancyRate = 0.92m,
            monthlyRevenue = 14320m
        };

        return Ok(summary);
    }

    [HttpGet("activities")]
    public IActionResult GetActivities([FromQuery] int limit = 20)
    {
        // TODO: Implement real logic
        var activities = new[]
        {
            new { type = "success", title = "Nouveau locataire ajouté", date = DateTime.UtcNow.AddMinutes(-5) },
            new { type = "info", title = "Contrat renouvelé", date = DateTime.UtcNow.AddHours(-2) },
            new { type = "warning", title = "Loyer en retard", date = DateTime.UtcNow.AddDays(-1) }
        };

        return Ok(activities.Take(limit));
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
