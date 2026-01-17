using LocaGuest.Application.Features.Invoices.Commands.GenerateMonthlyInvoices;
using LocaGuest.Application.Features.Invoices.Commands.GenerateInvoicePdf;
using LocaGuest.Application.Features.Invoices.Commands.MarkInvoiceAsPaid;
using LocaGuest.Application.Features.Invoices.Queries.ExportInvoices;
using LocaGuest.Application.Features.Invoices.Queries.GetFinancialStats;
using LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;
using LocaGuest.Application.Features.Invoices.Queries.GetOverdueInvoices;
using LocaGuest.Domain.Repositories;
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
    private readonly IUnitOfWork _unitOfWork;

    public InvoicesController(ILogger<InvoicesController> logger, IMediator mediator, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get invoices by tenant
    /// </summary>
    [HttpGet("tenant/{OccupantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTenant(Guid OccupantId)
    {
        var query = new GetInvoicesByTenantQuery(OccupantId);
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
        [FromQuery] Guid? OccupantId,
        [FromQuery] Guid? propertyId,
        [FromQuery] string format = "csv")
    {
        var query = new ExportInvoicesQuery(startDate, endDate, OccupantId, propertyId, format);
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

    /// <summary>
    /// Get (or generate) the invoice PDF for a rent invoice
    /// </summary>
    [HttpGet("{invoiceId:guid}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _unitOfWork.RentInvoices.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });

            if (!invoice.InvoiceDocumentId.HasValue)
            {
                var gen = await _mediator.Send(new GenerateInvoicePdfCommand(invoice.Id), cancellationToken);
                if (!gen.IsSuccess)
                    return BadRequest(new { message = gen.ErrorMessage });
            }

            if (!invoice.InvoiceDocumentId.HasValue)
                return BadRequest(new { message = "Invoice PDF not available" });

            var doc = await _unitOfWork.Documents.GetByIdAsync(invoice.InvoiceDocumentId.Value, cancellationToken);
            if (doc == null)
                return NotFound(new { message = "Invoice document not found" });

            if (!System.IO.File.Exists(doc.FilePath))
                return NotFound(new { message = "Invoice file not found on disk" });

            var bytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath, cancellationToken);
            return File(bytes, "application/pdf", doc.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice PDF for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Error getting invoice PDF", error = ex.Message });
        }
    }

    /// <summary>
    /// Force invoice PDF generation (returns documentId)
    /// </summary>
    [HttpPost("{invoiceId:guid}/generate-pdf")]
    public async Task<IActionResult> GenerateInvoicePdf(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateInvoicePdfCommand(invoiceId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { documentId = result.Data });
    }
}

public record MarkInvoiceAsPaidRequest(DateTime PaidDate, string? Notes);
