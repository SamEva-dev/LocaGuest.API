using Microsoft.AspNetCore.Mvc;
using LocaGuest.Application.Features.Billing.Commands.HandleStripeWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LocaGuest.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IMediator mediator,
        ILogger<StripeWebhookController> logger,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(stripeSignature))
            return BadRequest();
        var result = await _mediator.Send(new HandleStripeWebhookCommand
        {
            Payload = json,
            Signature = stripeSignature
        });

        if (!result.IsSuccess)
            return BadRequest();

        return Ok();
    }
}
