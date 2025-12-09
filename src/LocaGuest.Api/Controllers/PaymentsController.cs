using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Features.Payments.Commands.CreatePayment;
using LocaGuest.Application.Features.Payments.Commands.UpdatePayment;
using LocaGuest.Application.Features.Payments.Commands.VoidPayment;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsByTenant;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsByProperty;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentStats;
using LocaGuest.Application.Features.Payments.Queries.GetOverduePayments;
using LocaGuest.Application.Features.Payments.Queries.GetPaymentsDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        var command = new CreatePaymentCommand
        {
            TenantId = dto.TenantId,
            PropertyId = dto.PropertyId,
            ContractId = dto.ContractId,
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
}
