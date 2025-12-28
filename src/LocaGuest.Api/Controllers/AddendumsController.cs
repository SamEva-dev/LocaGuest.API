using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Addendums;
using LocaGuest.Application.Features.Addendums.Commands.DeleteAddendum;
using LocaGuest.Application.Features.Addendums.Commands.UpdateAddendum;
using LocaGuest.Application.Features.Addendums.Queries.GetAddendum;
using LocaGuest.Application.Features.Addendums.Queries.GetAddendums;
using LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AddendumsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AddendumsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddendumDto dto)
    {
        var command = new CreateAddendumCommand
        {
            ContractId = dto.ContractId,
            Type = dto.Type,
            EffectiveDate = dto.EffectiveDate,
            Reason = dto.Reason,
            Description = dto.Description,
            NewRent = dto.NewRent,
            NewCharges = dto.NewCharges,
            NewEndDate = dto.NewEndDate,
            OccupantChanges = dto.OccupantChanges,
            NewRoomId = dto.NewRoomId,
            NewClauses = dto.NewClauses,
            AttachedDocumentIds = dto.AttachedDocumentIds ?? new List<Guid>(),
            Notes = dto.Notes,
            SendEmail = dto.SendEmail,
            RequireSignature = dto.RequireSignature
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Addendum created successfully", addendumId = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? contractId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? signatureStatus = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var query = new GetAddendumsQuery
        {
            Page = page,
            PageSize = pageSize,
            ContractId = contractId,
            Type = type,
            SignatureStatus = signatureStatus,
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
        var result = await _mediator.Send(new GetAddendumQuery(id));
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddendumDto dto)
    {
        var command = new UpdateAddendumCommand
        {
            Id = id,
            EffectiveDate = dto.EffectiveDate,
            Reason = dto.Reason,
            Description = dto.Description,
            AttachedDocumentIds = dto.AttachedDocumentIds,
            Notes = dto.Notes
        };

        var result = await _mediator.Send(command);
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
        var result = await _mediator.Send(new DeleteAddendumCommand(id));
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Addendum deleted successfully", id });
    }
}
