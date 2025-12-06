using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v2/[controller]")]
public class PropertiesV2Controller : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<PropertiesV2Controller> _logger;

    public PropertiesV2Controller(LocaGuestDbContext context, ILogger<PropertiesV2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties(
        [FromQuery] string? status = null,
        [FromQuery] string? city = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Properties.AsQueryable();

        // Filtres
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Aggregates.PropertyAggregate.PropertyStatus>(status, true, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(p => p.City.Contains(city));
        }

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(p => p.Name.Contains(q) || p.Address.Contains(q));
        }

        var total = await query.CountAsync();
        var properties = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Address,
                p.City,
                p.Type,
                p.Status,
                p.Rent,
                p.Bedrooms,
                p.Bathrooms,
                p.Surface,
                ImageUrl = p.ImageUrls.Any() ? p.ImageUrls.First() : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = properties });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProperty(Guid id)
    {
        var property = await _context.Properties
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Address,
                p.City,
                p.PostalCode,
                p.Country,
                p.Type,
                p.Status,
                p.Rent,
                p.Bedrooms,
                p.Bathrooms,
                p.Surface,
                p.HasElevator,
                p.HasParking,
                p.Floor,
                p.IsFurnished,
                p.Charges,
                p.Deposit,
                p.Notes,
                p.ImageUrls,
                p.CreatedAt,
                p.CreatedBy
            })
            .FirstOrDefaultAsync();

        if (property == null)
            return NotFound(new { message = "Property not found" });

        return Ok(property);
    }

    /// <summary>
    /// DEPRECATED: Use PaymentsController instead
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [Obsolete("Use GET /api/payments/property/{id} instead")]
    public async Task<IActionResult> GetPropertyPayments(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        // TODO: Migrate to new PaymentAggregate system
        return BadRequest(new { message = "This endpoint is deprecated. Use GET /api/payments/property/{id} instead" });
    }

    /// <summary>
    /// Get contracts for property - UPDATED: PaymentsCount removed (use new payment system)
    /// </summary>
    [HttpGet("{id:guid}/contracts")]
    public async Task<IActionResult> GetPropertyContracts(Guid id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        var contracts = await _context.Contracts
            .Where(c => c.PropertyId == id)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                c.RenterTenantId,
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
    /// DEPRECATED: Use GET /api/payments/stats?propertyId={id} instead
    /// </summary>
    [HttpGet("{id:guid}/financial-summary")]
    [Obsolete("Use GET /api/payments/stats?propertyId={id} instead")]
    public async Task<IActionResult> GetPropertyFinancialSummary(Guid id)
    {
        // TODO: Migrate to new PaymentAggregate system
        return BadRequest(new { message = "This endpoint is deprecated. Use GET /api/payments/stats?propertyId={id} instead" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        var property = Domain.Aggregates.PropertyAggregate.Property.Create(
            request.Name,
            request.Address,
            request.City,
            request.Type,
            Domain.Aggregates.PropertyAggregate.PropertyUsageType.Complete, // Type par défaut
            request.Rent,
            request.Bedrooms,
            request.Bathrooms
        );

        if (!string.IsNullOrEmpty(request.PostalCode))
            property.GetType().GetProperty("PostalCode")!.SetValue(property, request.PostalCode);
        
        if (!string.IsNullOrEmpty(request.Country))
            property.GetType().GetProperty("Country")!.SetValue(property, request.Country);

        property.SetStatus(request.Status);

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, new { id = property.Id, name = property.Name });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProperty(Guid id, [FromBody] UpdatePropertyRequest request)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        property.UpdateDetails(
            name: request.Name,
            address: request.Address,
            rent: request.Rent,
            bedrooms: request.Bedrooms,
            bathrooms: request.Bathrooms
        );

        if (request.Status.HasValue)
            property.SetStatus(request.Status.Value);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Property updated successfully", id = property.Id });
    }
}

public record CreatePropertyRequest(
    string Name,
    string Address,
    string City,
    Domain.Aggregates.PropertyAggregate.PropertyType Type,
    decimal Rent,
    int Bedrooms,
    int Bathrooms,
    string? PostalCode = null,
    string? Country = null,
    Domain.Aggregates.PropertyAggregate.PropertyStatus Status = Domain.Aggregates.PropertyAggregate.PropertyStatus.Vacant
);

public record UpdatePropertyRequest(
    string? Name = null,
    string? Address = null,
    decimal? Rent = null,
    int? Bedrooms = null,
    int? Bathrooms = null,
    Domain.Aggregates.PropertyAggregate.PropertyStatus? Status = null
);
