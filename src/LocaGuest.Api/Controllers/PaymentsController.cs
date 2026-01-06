using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Features.Payments.Commands.CreatePayment;
using LocaGuest.Application.Features.Payments.Commands.UpdatePayment;
using LocaGuest.Application.Features.Payments.Commands.VoidPayment;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsByTenant;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsByProperty;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentStats;
using LocaGuest.Application.Features.Payments.Queries.GetOverduePayments;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsDashboard;
using LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocaGuest.Api.Authorization;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    [HttpPost]
   // [Authorize(Policy = Permissions.PaymentsWrite)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        var command = new CreatePaymentCommand
        {
            TenantId = dto.TenantId,
            PropertyId = dto.PropertyId,
            ContractId = dto.ContractId,
            PaymentType = dto.PaymentType,
            AmountDue = dto.AmountDue,
            AmountPaid = dto.AmountPaid,
            PaymentDate = dto.PaymentDate,
            ExpectedDate = dto.ExpectedDate,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetPaymentsByTenant), new { tenantId = dto.TenantId }, result.Data);
    }

    /// <summary>
    /// Get all payments for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentsByTenant(string tenantId)
    {
        var query = new GetPaymentsByTenantQuery { TenantId = tenantId };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all payments for a property
    /// </summary>
    [HttpGet("property/{propertyId}")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentsByProperty(string propertyId)
    {
        var query = new GetPaymentsByPropertyQuery { PropertyId = propertyId };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get payment statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PaymentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentStats(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? propertyId = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var query = new GetPaymentStatsQuery
        {
            TenantId = tenantId,
            PropertyId = propertyId,
            Month = month,
            Year = year
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Update an existing payment
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.PaymentsWrite)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePayment(string id, [FromBody] UpdatePaymentDto dto)
    {
        var command = new UpdatePaymentCommand
        {
            PaymentId = id,
            AmountPaid = dto.AmountPaid,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a payment (void it)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.PaymentsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        var command = new VoidPaymentCommand { PaymentId = id };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all overdue payments
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverduePayments(
        [FromQuery] Guid? propertyId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int? maxDaysLate = null)
    {
        var query = new GetOverduePaymentsQuery
        {
            PropertyId = propertyId,
            TenantId = tenantId,
            MaxDaysLate = maxDaysLate
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get payments dashboard with KPIs
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(PaymentsDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentsDashboard(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var query = new GetPaymentsDashboardQuery
        {
            Month = month,
            Year = year
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get (or generate) the quittance PDF for a payment
    /// </summary>
    [HttpGet("{paymentId:guid}/quittance")]
    public async Task<IActionResult> GetPaymentQuittance(Guid paymentId, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
                return NotFound(new { message = "Payment not found" });

            if (!payment.IsPaid() || !payment.PaymentDate.HasValue)
                return BadRequest(new { message = "Payment must be paid to generate a quittance" });

            if (!payment.ReceiptId.HasValue)
            {
                var gen = await _mediator.Send(new GeneratePaymentQuittanceCommand { PaymentId = payment.Id }, cancellationToken);
                if (!gen.IsSuccess)
                    return BadRequest(new { message = gen.ErrorMessage });
            }

            if (!payment.ReceiptId.HasValue)
                return BadRequest(new { message = "Receipt not available" });

            var doc = await _unitOfWork.Documents.GetByIdAsync(payment.ReceiptId.Value, cancellationToken);
            if (doc == null)
                return NotFound(new { message = "Quittance document not found" });

            if (!System.IO.File.Exists(doc.FilePath))
                return NotFound(new { message = "Quittance file not found on disk" });

            var bytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath, cancellationToken);
            return File(bytes, "application/pdf", doc.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quittance for payment {PaymentId}", paymentId);
            return StatusCode(500, new { message = "Error getting quittance", error = ex.Message });
        }
    }
}
