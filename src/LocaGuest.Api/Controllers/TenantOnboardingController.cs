using LocaGuest.Api.Authorization;
using LocaGuest.Api.Services;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using LocaGuest.Emailing.Abstractions;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LocaGuest.Api.Features.TenantOnboarding.CreateInvitation;
using LocaGuest.Api.Features.TenantOnboarding.GetInvitation;
using LocaGuest.Api.Features.TenantOnboarding.Submit;
using MediatR;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/tenant-onboarding")]
public sealed class TenantOnboardingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantOnboardingController> _logger;

    public TenantOnboardingController(
        IMediator mediator,
        ILogger<TenantOnboardingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("invitations")]
    [Authorize(Policy = Permissions.TenantsWrite)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        var orgClaim = User.FindFirst("organization_id")?.Value;
        if (!Guid.TryParse(orgClaim, out var orgId) || orgId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var result = await _mediator.Send(
                new CreateTenantOnboardingInvitationCommand(orgId, request.Email, request.PropertyId),
                ct);

            return Ok(new CreateInvitationResponse(result.Token, result.Link, result.ExpiresAtUtc));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant onboarding invitation request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create tenant onboarding invitation");
            return BadRequest(new { message = "Impossible de créer l'invitation. Veuillez réessayer." });
        }
    }

    [HttpGet("invitation")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInvitation([FromQuery] string token, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantOnboardingInvitationQuery(token), ct);

        if (!result.IsValid)
            return BadRequest(new { message = result.Message ?? "Lien invalide ou expiré." });

        return Ok(new GetInvitationResponse(
            Email: result.Invitation!.Email,
            PropertyId: result.Invitation.PropertyId,
            ExpiresAtUtc: result.Invitation.ExpiresAtUtc));
    }

    [HttpPost("submit")]
    [AllowAnonymous]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Submit(
        [FromForm] string token,
        [FromForm] string firstName,
        [FromForm] string lastName,
        [FromForm] string? phone,
        [FromForm] DateTime? dateOfBirth,
        [FromForm] string? address,
        [FromForm] string? city,
        [FromForm] string? postalCode,
        [FromForm] string? country,
        [FromForm] string? nationality,
        [FromForm] string? idNumber,
        [FromForm] string? emergencyContact,
        [FromForm] string? emergencyPhone,
        [FromForm] string? occupation,
        [FromForm] decimal? monthlyIncome,
        [FromForm] string? notes,
        [FromForm] DateTime? identityExpiryDate,
        IFormFile? identityDocument,
        IFormFile? addressProof,
        IFormFile? guarantyProof,
        IFormFile? guarantorIdentity,
        List<IFormFile>? incomeProofs,
        List<IFormFile>? guarantorIncomeProofs,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SubmitTenantOnboardingCommand(
                    Token: token,
                    FirstName: firstName,
                    LastName: lastName,
                    Phone: phone,
                    DateOfBirth: dateOfBirth,
                    Address: address,
                    City: city,
                    PostalCode: postalCode,
                    Country: country,
                    Nationality: nationality,
                    IdNumber: idNumber,
                    EmergencyContact: emergencyContact,
                    EmergencyPhone: emergencyPhone,
                    Occupation: occupation,
                    MonthlyIncome: monthlyIncome,
                    Notes: notes,
                    IdentityExpiryDate: identityExpiryDate,
                    IdentityDocument: identityDocument,
                    AddressProof: addressProof,
                    GuarantyProof: guarantyProof,
                    GuarantorIdentity: guarantorIdentity,
                    IncomeProofs: incomeProofs,
                    GuarantorIncomeProofs: guarantorIncomeProofs),
                ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message ?? "Impossible de valider votre dossier." });

            return Ok(new { occupantId = result.OccupantId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid onboarding submit payload");
            return BadRequest(new { message = "Certaines informations sont invalides. Merci de vérifier le formulaire." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid onboarding submit operation");
            return BadRequest(new { message = "Impossible de valider votre dossier. Merci de demander un nouveau lien à votre propriétaire." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant onboarding submit");
            if ((ex.Message ?? string.Empty).Contains("Cannot write DateTime with Kind=Unspecified", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Une date fournie dans le formulaire n'est pas valide. Merci de vérifier les champs de date puis réessayer."
                });
            }

            return StatusCode(500, new
            {
                message = "Une erreur est survenue lors de l'envoi du dossier. Veuillez réessayer plus tard."
            });
        }
    }
}

public sealed record CreateInvitationRequest(
    string Email,
    Guid? PropertyId);

public sealed record CreateInvitationResponse(
    string Token,
    string Link,
    DateTime ExpiresAtUtc);

public sealed record GetInvitationResponse(
    string Email,
    Guid? PropertyId,
    DateTime ExpiresAtUtc);
