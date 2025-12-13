using LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;
using LocaGuest.Application.Features.Organizations.Commands.DeleteOrganization;
using LocaGuest.Application.Features.Organizations.Commands.HardDeleteOrganization;
using LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;
using LocaGuest.Application.Features.Organizations.Queries.GetActiveOrganizations;
using LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;
using LocaGuest.Application.Features.Organizations.Queries.GetCurrentOrganization;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using LocaGuest.Application.Common.Interfaces;
using GetByIdDto = LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById.OrganizationDto;
using OrganizationDto = LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings.OrganizationDto;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsController> _logger;
    private readonly IFileStorageService _fileStorageService;

    public OrganizationsController(
        IMediator mediator, 
        ILogger<OrganizationsController> logger,
        IFileStorageService fileStorageService)
    {
        _mediator = mediator;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Get all organizations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<GetByIdDto>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(List<GetByIdDto>), StatusCodes.Status200OK)]
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
    /// Get current user's organization (with branding settings)
    /// </summary>
    [HttpGet("current")]
   // [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
   // [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentOrganization()
    {
        // Get user ID from claims (placeholder for now)
        var userId = GetUserId(); // TODO: Get from HttpContext.User claims

        var query = new GetCurrentOrganizationQuery { UserId = userId };
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetByIdDto), StatusCodes.Status200OK)]
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
    /// Upload organization logo
    /// </summary>
    [HttpPost("{id}/logo")]
    [RequestSizeLimit(2 * 1024 * 1024)] // 2MB max
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
    {
        try
        {
            // Validate file presence
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Validate file using service
            if (!_fileStorageService.ValidateFile(file.FileName, file.ContentType, file.Length))
            {
                return BadRequest(new
                {
                    error = "Invalid file. Only JPEG, PNG, SVG, and WebP images under 2MB are allowed."
                });
            }

            // Upload file with unique name
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            string logoUrl;
            using (var stream = file.OpenReadStream())
            {
                var relativePath = await _fileStorageService.SaveFileAsync(
                    stream,
                    uniqueFileName,
                    file.ContentType,
                    "logos" // subPath for organization logos
                );

                // Convert to web URL (remove wwwroot prefix)
                logoUrl = "/" + relativePath.Replace("\\", "/");
            }

            // Update organization with new logo URL
            var command = new UpdateOrganizationSettingsCommand
            {
                OrganizationId = id,
                LogoUrl = logoUrl
            };
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                // Delete uploaded file if update fails
                await _fileStorageService.DeleteFileAsync(logoUrl);
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation("Logo uploaded successfully for organization {OrganizationId}: {LogoUrl}", id, logoUrl);

            return Ok(new { logoUrl, message = "Logo uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for organization {OrganizationId}", id);
            return StatusCode(500, new { error = "An error occurred while uploading the logo" });
        }
    }

    ///// <summary>
    ///// Update organization settings (name, branding, etc.)
    ///// </summary>
    //[HttpPut("settings")]
    //[ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> UpdateOrganizationSettings([FromBody] UpdateOrganizationSettingsCommand command)
    //{
    //    var result = await _mediator.Send(command);

    //    if (result.IsFailure)
    //    {
    //        return BadRequest(new { error = result.ErrorMessage });
    //    }

    //    return Ok(result.Data);
    //}

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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}
