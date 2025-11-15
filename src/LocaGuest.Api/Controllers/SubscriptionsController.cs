using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ILocaGuestDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(
        ILocaGuestDbContext context,
        ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Récupère tous les plans disponibles
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Récupère l'abonnement actif de l'utilisateur
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var userId = GetUserId();
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

        if (subscription == null)
        {
            // Retourner le plan Free par défaut
            var freePlan = await _subscriptionService.GetPlanAsync(userId);
            return Ok(new
            {
                plan = freePlan,
                status = "free",
                isActive = true
            });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Récupère l'usage actuel de l'utilisateur
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage()
    {
        var userId = GetUserId();
        var plan = await _subscriptionService.GetPlanAsync(userId);

        var usage = new
        {
            scenarios = new
            {
                current = await _subscriptionService.GetUsageAsync(userId, "scenarios"),
                limit = plan.MaxScenarios,
                unlimited = plan.MaxScenarios == int.MaxValue
            },
            exports = new
            {
                current = await _subscriptionService.GetUsageAsync(userId, "exports"),
                limit = plan.HasUnlimitedExports ? int.MaxValue : plan.MaxExportsPerMonth,
                unlimited = plan.HasUnlimitedExports
            },
            aiSuggestions = new
            {
                current = await _subscriptionService.GetUsageAsync(userId, "ai_suggestions"),
                limit = plan.HasUnlimitedAi ? int.MaxValue : plan.MaxAiSuggestionsPerMonth,
                unlimited = plan.HasUnlimitedAi
            },
            shares = new
            {
                current = await _subscriptionService.GetUsageAsync(userId, "shares"),
                limit = plan.MaxShares,
                unlimited = plan.MaxShares == int.MaxValue
            }
        };

        return Ok(usage);
    }

    /// <summary>
    /// Vérifie si une feature est accessible
    /// </summary>
    [HttpGet("features/{featureName}")]
    public async Task<IActionResult> CheckFeature(string featureName)
    {
        var userId = GetUserId();
        var hasAccess = await _subscriptionService.CanAccessFeatureAsync(userId, featureName);

        return Ok(new { feature = featureName, hasAccess });
    }

    /// <summary>
    /// Vérifie le quota pour une dimension
    /// </summary>
    [HttpGet("quota/{dimension}")]
    public async Task<IActionResult> CheckQuota(string dimension)
    {
        var userId = GetUserId();
        var hasQuota = await _subscriptionService.CheckQuotaAsync(userId, dimension);
        var currentUsage = await _subscriptionService.GetUsageAsync(userId, dimension);

        return Ok(new
        {
            dimension,
            hasQuota,
            currentUsage
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}
