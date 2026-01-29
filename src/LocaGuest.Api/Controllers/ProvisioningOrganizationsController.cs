using LocaGuest.Application.Features.Provisioning.Organizations.Commands.DeactivateProvisioningOrganization;
using LocaGuest.Application.Features.Provisioning.Organizations.Commands.HardDeleteProvisioningOrganization;
using LocaGuest.Application.Features.Provisioning.Organizations.Commands.ProvisionOrganization;
using LocaGuest.Application.Features.Provisioning.Organizations.Commands.UpdateProvisioningOrganization;
using LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizationById;
using LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizations;
using LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizationSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/provisioning/organizations")]
[Authorize(Policy = "Provisioning")]
[EnableRateLimiting("ProvisioningLimiter")]
public sealed class ProvisioningOrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvisioningOrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ProvisionOrganizationResponseDto>> Provision(
        [FromBody] ProvisionOrganizationBody request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ProvisionOrganizationCommand(
                OrganizationName: request.OrganizationName,
                OrganizationEmail: request.OrganizationEmail,
                OrganizationPhone: request.OrganizationPhone,
                OwnerUserId: request.OwnerUserId,
                OwnerEmail: request.OwnerEmail,
                IdempotencyKey: Request.Headers["Idempotency-Key"].ToString()),
            ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}/sessions")]
    public async Task<ActionResult<List<ProvisioningOrganizationSessionDto>>> GetSessions(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProvisioningOrganizationSessionsQuery(id), ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProvisioningOrganizationDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProvisioningOrganizationsQuery(), ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProvisioningOrganizationDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProvisioningOrganizationByIdQuery(id), ct);

        if (result.IsFailure)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateProvisioningOrganizationDto>> Update(
        Guid id,
        [FromBody] UpdateProvisioningOrganizationBody body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProvisioningOrganizationCommand(
                OrganizationId: id,
                Name: body.Name,
                Email: body.Email,
                Phone: body.Phone,
                Status: body.Status),
            ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivateProvisioningOrganizationCommand(id), ct);

        if (result.IsFailure)
            return NotFound(new { error = result.ErrorMessage });

        return NoContent();
    }

    [HttpDelete("{id:guid}/permanent")]
    public async Task<IActionResult> HardDelete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new HardDeleteProvisioningOrganizationCommand(id), ct);

        if (result.IsFailure)
            return NotFound(new { error = result.ErrorMessage });

        return NoContent();
    }

    public sealed record ProvisionOrganizationBody(
        string OrganizationName,
        string OrganizationEmail,
        string? OrganizationPhone,
        string OwnerUserId,
        string OwnerEmail);

    public sealed record UpdateProvisioningOrganizationBody(
        string? Name,
        string? Email,
        string? Phone,
        string? Status);
}
