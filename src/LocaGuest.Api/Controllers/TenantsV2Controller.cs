using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v2/[controller]")]
public class TenantsV2Controller : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<TenantsV2Controller> _logger;

    public TenantsV2Controller(LocaGuestDbContext context, ILogger<TenantsV2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Tenants.AsQueryable();

        // Filtres
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(t => t.FullName.Contains(q) || t.Email.Contains(q));
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Aggregates.TenantAggregate.TenantStatus>(status, true, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        var total = await query.CountAsync();
        var tenants = await query
            .OrderBy(t => t.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.FullName,
                t.Email,
                t.Phone,
                t.MoveInDate,
                t.Status,
                ActiveContracts = _context.Contracts.Count(c => c.RenterTenantId == t.Id && c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active)
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = tenants });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _context.Tenants
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.FullName,
                t.Email,
                t.Phone,
                t.MoveInDate,
                t.Status,
                t.Notes,
                t.CreatedAt,
                t.CreatedBy,
                ActiveContracts = _context.Contracts.Count(c => c.RenterTenantId == t.Id && c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active),
                TotalContracts = _context.Contracts.Count(c => c.RenterTenantId == t.Id)
            })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        return Ok(tenant);
    }

    /// <summary>
    /// DEPRECATED: Use PaymentsController instead
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [Obsolete("Use GET /api/payments/tenant/{id} instead")]
    public async Task<IActionResult> GetTenantPayments(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        // TODO: Migrate to new PaymentAggregate system
        return BadRequest(new { message = "This endpoint is deprecated. Use GET /api/payments/tenant/{id} instead" });
    }

    [HttpGet("{id:guid}/contracts")]
    public async Task<IActionResult> GetTenantContracts(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        var contracts = await _context.Contracts
            .Where(c => c.RenterTenantId == id)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                PropertyName = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Name).FirstOrDefault(),
                PropertyAddress = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Address).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status
            })
            .ToListAsync();

        return Ok(contracts);
    }

    /// <summary>
    /// DEPRECATED: Use GET /api/payments/stats?tenantId={id} instead
    /// </summary>
    [HttpGet("{id:guid}/payment-stats")]
    [Obsolete("Use GET /api/payments/stats?tenantId={id} instead")]
    public async Task<IActionResult> GetTenantPaymentStats(Guid id)
    {
        // TODO: Migrate to new PaymentAggregate system
        return BadRequest(new { message = "This endpoint is deprecated. Use GET /api/payments/stats?tenantId={id} instead" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var tenant = Domain.Aggregates.TenantAggregate.Tenant.Create(
            request.FullName,
            request.Email,
            request.Phone
        );

        if (request.MoveInDate.HasValue)
            tenant.SetMoveInDate(request.MoveInDate.Value);

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new { id = tenant.Id, fullName = tenant.FullName });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        tenant.UpdateContact(request.Email, request.Phone);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Tenant updated successfully", id = tenant.Id });
    }
}

public record CreateTenantRequest(
    string FullName,
    string Email,
    string? Phone = null,
    DateTime? MoveInDate = null
);

public record UpdateTenantRequest(
    string? Email = null,
    string? Phone = null
);
