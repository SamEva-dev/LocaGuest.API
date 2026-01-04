using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LocaGuest.Application.Features.Billing.Commands.CreateCheckoutSession;
using LocaGuest.Application.Features.Billing.Commands.CreatePortalSession;
using LocaGuest.Application.Features.Billing.Queries.GetCheckoutSession;
using MediatR;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        IConfiguration configuration,
        IMediator mediator,
        ILogger<CheckoutController> logger)
    {
        _configuration = configuration;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Crée une session Checkout Stripe pour un plan
    /// </summary>
    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        var command = new CreateCheckoutSessionCommand
        {
            PlanId = request.PlanId.ToString(),
            IsAnnual = request.IsAnnual,
            SuccessUrl = _configuration["Stripe:SuccessUrl"],
            CancelUrl = _configuration["Stripe:CancelUrl"]
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            sessionId = result.Data!.SessionId,
            url = result.Data.CheckoutUrl
        });
    }

    /// <summary>
    /// Récupère les détails d'une session après paiement
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        var result = await _mediator.Send(new GetCheckoutSessionQuery { SessionId = sessionId });

        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.ErrorMessage });

        return Ok(new
        {
            status = result.Data!.Status,
            customerEmail = result.Data.CustomerEmail,
            subscriptionId = result.Data.SubscriptionId
        });
    }

    /// <summary>
    /// Crée un portail de gestion de l'abonnement
    /// </summary>
    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession()
    {
        var result = await _mediator.Send(new CreatePortalSessionCommand
        {
            ReturnUrl = _configuration["Stripe:CancelUrl"]
        });

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { url = result.Data!.Url });
    }
}

public record CreateCheckoutRequest(Guid PlanId, bool IsAnnual);
