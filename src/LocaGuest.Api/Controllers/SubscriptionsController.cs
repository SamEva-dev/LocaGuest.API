using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LocaGuest.Application.Features.Subscriptions.Queries.CheckFeature;
using LocaGuest.Application.Features.Subscriptions.Queries.CheckQuota;
using LocaGuest.Application.Features.Subscriptions.Queries.GetActivePlans;
using LocaGuest.Application.Features.Subscriptions.Queries.GetCurrentSubscription;
using LocaGuest.Application.Features.Subscriptions.Queries.GetUsage;
using MediatR;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Récupère tous les plans disponibles
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var result = await _mediator.Send(new GetActivePlansQuery());

        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupère l'abonnement actif de l'utilisateur
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var result = await _mediator.Send(new GetCurrentSubscriptionQuery());

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorMessage, "User not authenticated", StringComparison.Ordinal))
                return Unauthorized();

            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupère l'usage actuel de l'utilisateur
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage()
    {
        var result = await _mediator.Send(new GetUsageQuery());

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorMessage, "User not authenticated", StringComparison.Ordinal))
                return Unauthorized();

            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Vérifie si une feature est accessible
    /// </summary>
    [HttpGet("features/{featureName}")]
    public async Task<IActionResult> CheckFeature(string featureName)
    {
        var result = await _mediator.Send(new CheckFeatureQuery(featureName));

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorMessage, "User not authenticated", StringComparison.Ordinal))
                return Unauthorized();

            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Vérifie le quota pour une dimension
    /// </summary>
    [HttpGet("quota/{dimension}")]
    public async Task<IActionResult> CheckQuota(string dimension)
    {
        var result = await _mediator.Send(new CheckQuotaQuery(dimension));

        if (!result.IsSuccess)
        {
            if (string.Equals(result.ErrorMessage, "User not authenticated", StringComparison.Ordinal))
                return Unauthorized();

            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }
}
