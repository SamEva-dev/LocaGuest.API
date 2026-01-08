using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using LocaGuest.Application.Features.Contracts.Commands.RecordPayment;
using LocaGuest.Application.Features.Contracts.Commands.TerminateContract;
using LocaGuest.Application.Features.Contracts.Commands.MarkContractAsSigned;
using LocaGuest.Application.Features.Contracts.Commands.ActivateContract;
using LocaGuest.Application.Features.Contracts.Commands.MarkContractAsExpired;
using LocaGuest.Application.Features.Contracts.Commands.CancelContract;
using LocaGuest.Application.Features.Contracts.Queries.GetContractTerminationEligibility;
using LocaGuest.Application.Features.Contracts.Commands.UpdateContract;
using LocaGuest.Application.Features.Contracts.Commands.DeleteContract;
using LocaGuest.Application.Features.Contracts.Commands.RenewContract;
using LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;
using LocaGuest.Application.Features.Contracts.Commands.GiveNotice;
using LocaGuest.Application.Features.Contracts.Commands.CancelNotice;
using LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContractStats;
using LocaGuest.Application.Features.Contracts.Queries.GetContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContract;
using LocaGuest.Application.Features.Contracts.Queries.GetContractsByTenant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using LocaGuest.Api.Authorization;
using LocaGuest.Application.Services;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContractsController> _logger;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;

    public ContractsController(
        IMediator mediator,
        ILogger<ContractsController> logger,
        IEffectiveContractStateResolver effectiveContractStateResolver)
    {
        _mediator = mediator;
        _logger = logger;
        _effectiveContractStateResolver = effectiveContractStateResolver;
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

    [HttpGet("{id:guid}/effective-state")]
    public async Task<IActionResult> GetEffectiveState(
        Guid id,
        [FromQuery] DateTime? dateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var d = dateUtc ?? DateTime.UtcNow;

        var result = await _effectiveContractStateResolver.ResolveAsync(id, d, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            if ((result.ErrorMessage ?? string.Empty).Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage ?? "Error resolving effective contract state" });
        }

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

    /// <summary>
    /// Get all contracts for a specific occupant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(List<LocaGuest.Application.DTOs.Contracts.ContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractsByTenant(string tenantId)
    {
        var query = new GetContractsByTenantQuery { TenantId = tenantId };
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
    [Authorize(Policy = Permissions.ContractsWrite)]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetContract), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Policy = Permissions.PaymentsWrite)]
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
    [Authorize(Policy = Permissions.ContractsWrite)]
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

    [HttpGet("{id:guid}/termination-eligibility")]
    public async Task<IActionResult> GetTerminationEligibility(Guid id)
    {
        var result = await _mediator.Send(new GetContractTerminationEligibilityQuery(id));
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.ContractsWrite)]
    public async Task<IActionResult> CancelContract(Guid id)
    {
        var result = await _mediator.Send(new CancelContractCommand(id));
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Contract cancelled successfully", id });
    }

    [HttpPut("{id:guid}/mark-signed")]
    [Authorize(Policy = Permissions.ContractsWrite)]
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
    [Authorize(Policy = Permissions.ContractsWrite)]
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
    [Authorize(Policy = Permissions.ContractsWrite)]
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
    [Authorize(Policy = Permissions.ContractsWrite)]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractRequest request)
    {
        var command = new UpdateContractCommand
        {
            ContractId = id,
            TenantId = request.TenantId,
            TenantIdIsSet = request.TenantIdIsSet,
            PropertyId = request.PropertyId,
            PropertyIdIsSet = request.PropertyIdIsSet,
            RoomId = request.RoomId,
            RoomIdIsSet = request.RoomIdIsSet,
            Type = request.Type,
            TypeIsSet = request.TypeIsSet,
            StartDate = request.StartDate,
            StartDateIsSet = request.StartDateIsSet,
            EndDate = request.EndDate,
            EndDateIsSet = request.EndDateIsSet,
            Rent = request.Rent,
            RentIsSet = request.RentIsSet,
            Charges = request.Charges,
            ChargesIsSet = request.ChargesIsSet,
            Deposit = request.Deposit,
            DepositIsSet = request.DepositIsSet
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result);
        
        return Ok(new { message = "Contract updated successfully", id });
    }

    public sealed class UpdateContractRequest
    {
        private Guid? _tenantId;
        public Guid? TenantId { get => _tenantId; set { _tenantId = value; TenantIdIsSet = true; } }
        [JsonIgnore]
        public bool TenantIdIsSet { get; private set; }

        private Guid? _propertyId;
        public Guid? PropertyId { get => _propertyId; set { _propertyId = value; PropertyIdIsSet = true; } }
        [JsonIgnore]
        public bool PropertyIdIsSet { get; private set; }

        private Guid? _roomId;
        public Guid? RoomId { get => _roomId; set { _roomId = value; RoomIdIsSet = true; } }
        [JsonIgnore]
        public bool RoomIdIsSet { get; private set; }

        private string? _type;
        public string? Type { get => _type; set { _type = value; TypeIsSet = true; } }
        [JsonIgnore]
        public bool TypeIsSet { get; private set; }

        private DateTime? _startDate;
        public DateTime? StartDate { get => _startDate; set { _startDate = value; StartDateIsSet = true; } }
        [JsonIgnore]
        public bool StartDateIsSet { get; private set; }

        private DateTime? _endDate;
        public DateTime? EndDate { get => _endDate; set { _endDate = value; EndDateIsSet = true; } }
        [JsonIgnore]
        public bool EndDateIsSet { get; private set; }

        private decimal? _rent;
        public decimal? Rent { get => _rent; set { _rent = value; RentIsSet = true; } }
        [JsonIgnore]
        public bool RentIsSet { get; private set; }

        private decimal? _charges;
        public decimal? Charges { get => _charges; set { _charges = value; ChargesIsSet = true; } }
        [JsonIgnore]
        public bool ChargesIsSet { get; private set; }

        private decimal? _deposit;
        public decimal? Deposit { get => _deposit; set { _deposit = value; DepositIsSet = true; } }
        [JsonIgnore]
        public bool DepositIsSet { get; private set; }
    }
    
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

        return Ok(new
        {
            message = "Contract deleted successfully",
            id,
            deletedPayments = result.Data?.DeletedPayments ?? 0,
            deletedDocuments = result.Data?.DeletedDocuments ?? 0
        });
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
    
    /// <summary>
    /// Créer un avenant au contrat
    /// POST /api/contracts/{id}/addendum
    /// </summary>
    [HttpPost("{id:guid}/addendum")]
    public async Task<IActionResult> CreateAddendum(Guid id, [FromBody] CreateAddendumRequest request)
    {
        var command = new CreateAddendumCommand
        {
            ContractId = id,
            Type = request.Type,
            EffectiveDate = request.EffectiveDate,
            Reason = request.Reason,
            Description = request.Description,
            NewRent = request.NewRent,
            NewCharges = request.NewCharges,
            NewEndDate = request.NewEndDate,
            OccupantChanges = request.OccupantChanges,
            NewRoomId = request.NewRoomId,
            NewClauses = request.NewClauses,
            AttachedDocumentIds = request.AttachedDocumentIds ?? new List<Guid>(),
            Notes = request.Notes,
            SendEmail = request.SendEmail,
            RequireSignature = request.RequireSignature
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Addendum created successfully", addendumId = result.Data });
    }

    /// <summary>
    /// Donner un préavis sur un contrat
    /// PUT /api/contracts/{id}/notice
    /// </summary>
    [HttpPut("{id:guid}/notice")]
    public async Task<IActionResult> GiveNotice(Guid id, [FromBody] GiveNoticeRequest request)
    {
        var command = new GiveNoticeCommand
        {
            ContractId = id,
            NoticeDate = request.NoticeDate,
            NoticeEndDate = request.NoticeEndDate,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Notice registered successfully", id });
    }

    /// <summary>
    /// Annuler le préavis sur un contrat
    /// PUT /api/contracts/{id}/notice/cancel
    /// </summary>
    [HttpPut("{id:guid}/notice/cancel")]
    public async Task<IActionResult> CancelNotice(Guid id)
    {
        var result = await _mediator.Send(new CancelNoticeCommand { ContractId = id });
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Notice cancelled successfully", id });
    }

    public record GiveNoticeRequest(
        DateTime NoticeDate,
        DateTime NoticeEndDate,
        string Reason
    );
    
    public record CreateAddendumRequest(
        string Type,
        DateTime EffectiveDate,
        string Reason,
        string Description,
        decimal? NewRent,
        decimal? NewCharges,
        DateTime? NewEndDate,
        JsonElement? OccupantChanges,
        Guid? NewRoomId,
        string? NewClauses,
        List<Guid>? AttachedDocumentIds,
        string? Notes,
        bool SendEmail,
        bool RequireSignature
    );
}
