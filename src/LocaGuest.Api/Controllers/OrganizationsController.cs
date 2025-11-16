using LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Create a new organization (called by AuthGate during registration)
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Called by AuthGate service, not by user directly
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(result);
    }
}
