using LocaGuest.Application.Features.Billing.Commands.CancelSubscription;
using LocaGuest.Application.Features.Billing.Commands.CreateCheckoutSession;
using LocaGuest.Application.Features.Billing.Queries.GetBillingInvoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IMediator mediator, ILogger<BillingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a Stripe Checkout session for a plan
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get billing invoices for current user
    /// </summary>
    [HttpGet("invoices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices()
    {
        var query = new GetBillingInvoicesQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Cancel the current subscription
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        var command = new CancelSubscriptionCommand { CancelImmediately = request.CancelImmediately };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Abonnement annulé avec succès" });
    }

    /// <summary>
    /// Get Stripe customer portal URL
    /// </summary>
    [HttpGet("portal-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerPortalUrl([FromQuery] string returnUrl = "/settings/billing")
    {
        // TODO: Implement GetPortalUrlQuery
        return Ok(new { url = "https://billing.stripe.com/session/..." });
    }
}

public record CreateCheckoutSessionRequest(
    string PlanId,
    bool IsAnnual,
    string? SuccessUrl,
    string? CancelUrl
);

public record CancelSubscriptionRequest(
    bool CancelImmediately = false
);
