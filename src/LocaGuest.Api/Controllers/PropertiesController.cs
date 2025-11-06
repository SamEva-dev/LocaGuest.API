using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(LocaGuestDbContext context, ILogger<PropertiesController> logger)
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
                p.ZipCode,
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

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPropertyPayments(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        var query = _context.Payments
            .Include(p => p)
            .Where(p => _context.Contracts
                .Any(c => c.PropertyId == id && c.Payments.Any(pay => pay.Id == p.Id)));

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new
            {
                p.Id,
                p.Amount,
                p.PaymentDate,
                p.Method,
                p.Status,
                p.ContractId
            })
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id:guid}/contracts")]
    public async Task<IActionResult> GetPropertyContracts(Guid id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        var contracts = await _context.Contracts
            .Where(c => c.PropertyId == id)
            .Include(c => c.Payments)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.TenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.TenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status,
                PaymentsCount = c.Payments.Count
            })
            .ToListAsync();

        return Ok(contracts);
    }

    [HttpGet("{id:guid}/financial-summary")]
    public async Task<IActionResult> GetPropertyFinancialSummary(Guid id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
            return NotFound(new { message = "Property not found" });

        var totalPayments = await _context.Payments
            .Where(p => _context.Contracts.Any(c => c.PropertyId == id && c.Payments.Any(pay => pay.Id == p.Id)))
            .SumAsync(p => p.Amount);

        var lastPayment = await _context.Payments
            .Where(p => _context.Contracts.Any(c => c.PropertyId == id && c.Payments.Any(pay => pay.Id == p.Id)))
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new { p.Amount, p.PaymentDate })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            propertyId = id,
            totalRevenue = totalPayments,
            monthlyRent = property.Rent,
            lastPayment = lastPayment,
            occupancyRate = property.Status == Domain.Aggregates.PropertyAggregate.PropertyStatus.Occupied ? 1.0m : 0.0m
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        var property = Domain.Aggregates.PropertyAggregate.Property.Create(
            request.Name,
            request.Address,
            request.City,
            request.Type,
            request.Rent,
            request.Bedrooms,
            request.Bathrooms
        );

        if (!string.IsNullOrEmpty(request.ZipCode))
            property.GetType().GetProperty("ZipCode")!.SetValue(property, request.ZipCode);
        
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
    string? ZipCode = null,
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
