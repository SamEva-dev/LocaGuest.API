using LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContractStats;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<ContractsController> _logger;
    private readonly IMediator _mediator;

    public ContractsController(
        LocaGuestDbContext context, 
        ILogger<ContractsController> logger,
        IMediator mediator)
    {
        _context = context;
        _logger = logger;
        _mediator = mediator;
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
        var query = _context.Contracts.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Aggregates.ContractAggregate.ContractStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        var total = await query.CountAsync();
        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                PropertyName = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Name).FirstOrDefault(),
                c.TenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.TenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = contracts });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var contract = await _context.Contracts
            .Include(c => c.Payments)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                PropertyName = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Name).FirstOrDefault(),
                c.TenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.TenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status,
                c.Notes,
                PaymentsCount = c.Payments.Count
            })
            .FirstOrDefaultAsync();

        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        return Ok(contract);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        // Vérifier que la propriété et le locataire existent
        var property = await _context.Properties.FindAsync(request.PropertyId);
        if (property == null)
            return BadRequest(new { message = "Property not found" });

        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null)
            return BadRequest(new { message = "Tenant not found" });

        var contract = Domain.Aggregates.ContractAggregate.Contract.Create(
            request.PropertyId,
            request.TenantId,
            request.Type,
            request.StartDate,
            request.EndDate,
            request.Rent,
            request.Deposit
        );

        // Marquer la propriété comme occupée
        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Occupied);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContract), new { id = contract.Id }, new { id = contract.Id, propertyId = contract.PropertyId, tenantId = contract.TenantId });
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var contract = await _context.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        var payment = contract.RecordPayment(
            request.Amount,
            request.PaymentDate,
            request.Method
        );

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Payment recorded successfully", 
            paymentId = payment.Id,
            amount = payment.Amount,
            date = payment.PaymentDate
        });
    }

    [HttpPut("{id:guid}/terminate")]
    public async Task<IActionResult> TerminateContract(Guid id, [FromBody] TerminateContractRequest request)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        contract.Terminate(request.TerminationDate);

        // Si demandé, marquer la propriété comme vacante
        if (request.MarkPropertyVacant)
        {
            var property = await _context.Properties.FindAsync(contract.PropertyId);
            if (property != null)
            {
                property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Vacant);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Contract terminated successfully", id = contract.Id });
    }
}

public record CreateContractRequest(
    Guid PropertyId,
    Guid TenantId,
    Domain.Aggregates.ContractAggregate.ContractType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Rent,
    decimal? Deposit = null
);

public record RecordPaymentRequest(
    decimal Amount,
    DateTime PaymentDate,
    Domain.Aggregates.ContractAggregate.PaymentMethod Method
);

public record TerminateContractRequest(
    DateTime TerminationDate,
    bool MarkPropertyVacant = true
);
