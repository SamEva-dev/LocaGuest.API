using LocaGuest.Application.Features.Audit.AuditLogs.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/provisioning/auditlogs")]
[Authorize(Policy = "Provisioning")]
[EnableRateLimiting("ProvisioningLimiter")]
public sealed class ProvisioningAuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvisioningAuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OrganizationId = organizationId,
            CorrelationId = correlationId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }
}
