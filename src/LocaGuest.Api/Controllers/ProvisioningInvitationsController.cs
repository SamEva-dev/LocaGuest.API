using LocaGuest.Application.Features.Provisioning.Invitations.Commands.ConsumeInvitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/provisioning/invitations")]
[Authorize(Policy = "Provisioning")]
[EnableRateLimiting("ProvisioningLimiter")]
public sealed class ProvisioningInvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvisioningInvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("consume")]
    public async Task<ActionResult<ConsumeInvitationResponseDto>> Consume(
        [FromBody] ConsumeInvitationBody request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { message = "Idempotency-Key header is required." });

        var result = await _mediator.Send(
            new ConsumeInvitationCommand(
                Token: request.Token,
                UserId: request.UserId,
                UserEmail: request.UserEmail,
                IdempotencyKey: idempotencyKey!),
            ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }
}

public sealed record ConsumeInvitationBody(
    string Token,
    string UserId,
    string UserEmail);
