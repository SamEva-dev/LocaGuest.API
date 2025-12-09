using LocaGuest.Application.Features.Invoices.Commands.GenerateMonthlyInvoices;
using LocaGuest.Application.Features.Invoices.Commands.MarkInvoiceAsPaid;
using LocaGuest.Application.Features.Invoices.Queries.ExportInvoices;
using LocaGuest.Application.Features.Invoices.Queries.GetFinancialStats;
using LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;
using LocaGuest.Application.Features.Invoices.Queries.GetOverdueInvoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly ILogger<InvoicesController> _logger;
    private readonly IMediator _mediator;

    public InvoicesController(ILogger<InvoicesController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Get invoices by tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTenant(Guid tenantId)
    {
        var query = new GetInvoicesByTenantQuery(tenantId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all overdue invoices
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue()
    {
        var query = new GetOverdueInvoicesQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get financial statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialStats([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = new GetFinancialStatsQuery(year, month);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Export invoices to CSV/Excel
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? propertyId,
        [FromQuery] string format = "csv")
    {
        var query = new ExportInvoicesQuery(startDate, endDate, tenantId, propertyId, format);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return File(result.Data!.FileContent, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Generate invoices for a specific month
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateMonthlyInvoices([FromBody] GenerateMonthlyInvoicesCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Mark an invoice as paid
    /// </summary>
    [HttpPost("{invoiceId}/mark-paid")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsPaid(Guid invoiceId, [FromBody] MarkInvoiceAsPaidRequest request)
    {
        var command = new MarkInvoiceAsPaidCommand(invoiceId, request.PaidDate, request.Notes);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Facture marquée comme payée avec succès" });
    }
}

public record MarkInvoiceAsPaidRequest(DateTime PaidDate, string? Notes);
