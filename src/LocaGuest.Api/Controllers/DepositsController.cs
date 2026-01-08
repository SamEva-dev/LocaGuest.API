using LocaGuest.Api.Authorization;
using LocaGuest.Application.Features.Deposits.Commands.RecordDepositReceived;
using LocaGuest.Application.Features.Deposits.Queries.GetDepositByContract;
using LocaGuest.Application.Features.Deposits.Queries.GetDepositReceiptByContract;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepositsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepositsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Récupère la caution (deposit) d'un contrat
    /// GET /api/deposits/contract/{contractId}
    /// </summary>
    [HttpGet("contract/{contractId:guid}")]
    [Authorize(Policy = Permissions.DepositsRead)]
    public async Task<IActionResult> GetByContract(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDepositByContractQuery(contractId), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    public record ReceiveDepositRequest(decimal Amount, DateTime DateUtc, string? Reference);

    [HttpPost("contract/{contractId:guid}/receive")]
    [Authorize(Policy = Permissions.DepositsWrite)]
    public async Task<IActionResult> Receive(Guid contractId, [FromBody] ReceiveDepositRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RecordDepositReceivedCommand(contractId, request.Amount, request.DateUtc, request.Reference),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { depositId = result.Data });
    }

    [HttpGet("contract/{contractId:guid}/receipt")]
    [Authorize(Policy = Permissions.DepositsRead)]
    public async Task<IActionResult> GetReceipt(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDepositReceiptByContractQuery(contractId), cancellationToken);
        if (!result.IsSuccess || result.Data == null)
            return BadRequest(new { message = result.ErrorMessage ?? "Receipt not available" });

        return File(result.Data, "application/pdf", $"Recu_Caution_{contractId}.pdf");
    }
}
