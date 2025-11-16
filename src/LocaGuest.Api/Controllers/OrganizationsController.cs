using LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;
using LocaGuest.Application.Features.Organizations.Commands.DeleteOrganization;
using LocaGuest.Application.Features.Organizations.Commands.HardDeleteOrganization;
using LocaGuest.Application.Features.Organizations.Queries.GetActiveOrganizations;
using LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
///[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(IMediator mediator, ILogger<OrganizationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all organizations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrganizations()
    {
        var query = new GetAllOrganizationsQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get only active organizations (filtered)
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveOrganizations()
    {
        var query = new GetActiveOrganizationsQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationById(Guid id)
    {
        var query = new GetOrganizationByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new organization (called by AuthGate during registration)
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Called by AuthGate service, not by user directly
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Delete (deactivate) an organization - Soft Delete
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var command = new DeleteOrganizationCommand(id);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogInformation("Organization {OrganizationId} soft deleted (deactivated)", id);
        return NoContent();
    }

    /// <summary>
    /// Permanently delete an organization - Hard Delete (CANNOT BE UNDONE!)
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HardDeleteOrganization(Guid id)
    {
        var command = new HardDeleteOrganizationCommand(id);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        _logger.LogWarning("Organization {OrganizationId} PERMANENTLY deleted", id);
        return NoContent();
    }
}
