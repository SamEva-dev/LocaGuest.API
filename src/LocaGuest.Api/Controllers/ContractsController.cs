using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using LocaGuest.Application.Features.Contracts.Commands.RecordPayment;
using LocaGuest.Application.Features.Contracts.Commands.TerminateContract;
using LocaGuest.Application.Features.Contracts.Commands.MarkContractAsSigned;
using LocaGuest.Application.Features.Contracts.Commands.ActivateContract;
using LocaGuest.Application.Features.Contracts.Commands.MarkContractAsExpired;
using LocaGuest.Application.Features.Contracts.Commands.UpdateContract;
using LocaGuest.Application.Features.Contracts.Commands.DeleteContract;
using LocaGuest.Application.Features.Contracts.Commands.RenewContract;
using LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContractStats;
using LocaGuest.Application.Features.Contracts.Queries.GetContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContract;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(
        IMediator mediator,
        ILogger<ContractsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetContractStats()
    {
        var query = new GetContractStatsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllContracts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var query = new GetAllContractsQuery
        {
            SearchTerm = searchTerm,
            Status = status,
            Type = type
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetContractsQuery
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var query = new GetContractQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetContract), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentCommand command)
    {
        // Ensure contractId matches route parameter
        if (command.ContractId != id)
            return BadRequest(new { message = "Contract ID mismatch" });
            
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}/terminate")]
    public async Task<IActionResult> TerminateContract(Guid id, [FromBody] TerminateContractCommand command)
    {
        // Ensure contractId matches route parameter
        if (command.ContractId != id)
            return BadRequest(new { message = "Contract ID mismatch" });
            
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract terminated successfully", id });
    }

    [HttpPut("{id:guid}/mark-signed")]
    public async Task<IActionResult> MarkContractAsSigned(Guid id, [FromBody] MarkContractAsSignedCommand command)
    {
        // Ensure contractId matches route parameter
        if (command.ContractId != id)
            return BadRequest(new { message = "Contract ID mismatch" });
            
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract marked as signed successfully", id });
    }
    
    /// <summary>
    /// Activer manuellement un contrat signé (normalement fait automatiquement par le BackgroundService)
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> ActivateContract(Guid id)
    {
        var command = new ActivateContractCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract activated successfully", id });
    }
    
    /// <summary>
    /// Marquer manuellement un contrat comme expiré (normalement fait automatiquement par le BackgroundService)
    /// </summary>
    [HttpPut("{id:guid}/mark-expired")]
    public async Task<IActionResult> MarkContractAsExpired(Guid id)
    {
        var command = new MarkContractAsExpiredCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract marked as expired successfully", id });
    }
    
    /// <summary>
    /// Mettre à jour un contrat Draft
    /// PUT /api/contracts/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractRequest request)
    {
        var command = new UpdateContractCommand
        {
            ContractId = id,
            TenantId = request.TenantId,
            PropertyId = request.PropertyId,
            RoomId = request.RoomId,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Rent = request.Rent,
            Charges = request.Charges ?? 0,
            Deposit = request.Deposit
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result);
        
        return Ok(new { message = "Contract updated successfully", id });
    }
    
    public record UpdateContractRequest(
        Guid TenantId,
        Guid PropertyId,
        Guid? RoomId,
        string Type,
        DateTime StartDate,
        DateTime EndDate,
        decimal Rent,
        decimal? Charges,
        decimal? Deposit
    );
    
    /// <summary>
    /// Supprimer un contrat (uniquement les contrats Draft ou Cancelled)
    /// DELETE /api/contracts/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteContract(Guid id)
    {
        var command = new DeleteContractCommand(id);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract deleted successfully", id });
    }
    
    /// <summary>
    /// Renouveler un contrat existant
    /// POST /api/contracts/{id}/renew
    /// </summary>
    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> RenewContract(Guid id, [FromBody] RenewContractRequest request)
    {
        var command = new RenewContractCommand
        {
            ContractId = id,
            NewStartDate = request.NewStartDate,
            NewEndDate = request.NewEndDate,
            ContractType = request.ContractType,
            NewRent = request.NewRent,
            NewCharges = request.NewCharges,
            PreviousIRL = request.PreviousIRL,
            CurrentIRL = request.CurrentIRL,
            Deposit = request.Deposit,
            CustomClauses = request.CustomClauses,
            Notes = request.Notes,
            TacitRenewal = request.TacitRenewal,
            AttachedDocumentIds = request.AttachedDocumentIds ?? new List<Guid>()
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Contract renewed successfully", newContractId = result.Data });
    }
    
    public record RenewContractRequest(
        DateTime NewStartDate,
        DateTime NewEndDate,
        string ContractType,
        decimal NewRent,
        decimal NewCharges,
        decimal? PreviousIRL,
        decimal? CurrentIRL,
        decimal? Deposit,
        string? CustomClauses,
        string? Notes,
        bool TacitRenewal,
        List<Guid>? AttachedDocumentIds
    );
}
