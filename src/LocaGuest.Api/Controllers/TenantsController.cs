using LocaGuest.Application.Features.Tenants.Commands.CreateTenant;
using LocaGuest.Application.Features.Tenants.Queries.GetTenants;
using LocaGuest.Application.Features.Tenants.Queries.GetTenant;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;
    private readonly LocaGuestDbContext _context;

    public TenantsController(
        IMediator mediator, 
        ILogger<TenantsController> logger,
        LocaGuestDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants([FromQuery] GetTenantsQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetTenants), new { id = result.Data!.Id }, result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(string id)
    {
        var query = new GetTenantQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}/payments")]
    public IActionResult GetTenantPayments(string id)
    {
        // TODO: Implement GetTenantPaymentsQuery
        // For now, return empty list
        return Ok(new object[0]);
    }

    [HttpGet("{id}/contracts")]
    public async Task<IActionResult> GetTenantContracts(string id)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        var tenant = await _context.Tenants.FindAsync(tenantGuid);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        var contracts = await _context.Contracts
            .Where(c => c.RenterTenantId == tenantGuid)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.Code,
                TenantId = c.RenterTenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.RenterTenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status,
                PaymentsCount = c.Payments.Count
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} contracts for tenant {TenantId}", contracts.Count, id);
        return Ok(contracts);
    }

    [HttpGet("{id}/payment-stats")]
    public async Task<IActionResult> GetPaymentStats(string id)
    {
        if (!Guid.TryParse(id, out var tenantGuid))
            return BadRequest(new { message = "Invalid tenant ID format" });

        var tenant = await _context.Tenants.FindAsync(tenantGuid);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        var totalPaid = await _context.Payments
            .Where(p => _context.Contracts.Any(c => c.RenterTenantId == tenantGuid && c.Payments.Any(pay => pay.Id == p.Id)))
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var totalPayments = await _context.Payments
            .Where(p => _context.Contracts.Any(c => c.RenterTenantId == tenantGuid && c.Payments.Any(pay => pay.Id == p.Id)))
            .CountAsync();

        var latePayments = await _context.Payments
            .Where(p => _context.Contracts.Any(c => c.RenterTenantId == tenantGuid && c.Payments.Any(pay => pay.Id == p.Id))
                        && p.Status == Domain.Aggregates.ContractAggregate.PaymentStatus.Late)
            .CountAsync();

        return Ok(new
        {
            tenantId = id,
            totalPaid,
            totalPayments,
            latePayments,
            onTimeRate = totalPayments > 0 ? (decimal)(totalPayments - latePayments) / totalPayments : 1.0m
        });
    }
}
