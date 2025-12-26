using LocaGuest.Application.Features.Audit.CommandAuditLogs.Commands.DeleteCommandAuditLog;
using LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLog;
using LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CommandAuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommandAuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? commandName = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? success = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var query = new GetCommandAuditLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            CommandName = commandName,
            UserId = userId,
            TenantId = tenantId,
            Success = success,
            CorrelationId = correlationId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };

        var result = await _mediator.Send(query);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCommandAuditLogQuery(id));
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCommandAuditLogCommand(id));
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Command audit log deleted successfully", id });
    }
}
