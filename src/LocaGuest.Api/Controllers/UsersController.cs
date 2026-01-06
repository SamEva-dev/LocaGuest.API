using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using LocaGuest.Application.Features.Users.Queries.GetUserProfile;
using LocaGuest.Application.Features.Users.Commands.UpdateUserProfile;
using LocaGuest.Application.Features.Users.Queries.GetPreferences;
using LocaGuest.Application.Features.Users.Commands.UpdatePreferences;
using LocaGuest.Application.Features.Users.Queries.GetNotifications;
using LocaGuest.Application.Features.Users.Commands.UpdateNotifications;
using LocaGuest.Application.Features.Users.Commands.UpdateUserPhoto;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Api.Authorization;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IMediator _mediator;
    private readonly IAuthGateClient _authGateClient;

    public UsersController(ILogger<UsersController> logger, IMediator mediator, IAuthGateClient authGateClient)
    {
        _logger = logger;
        _mediator = mediator;
        _authGateClient = authGateClient;
    }

    /// <summary>
    /// Get all users (from AuthGate, via HttpClient call)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.TeamRead)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        try
        {
            var (statusCode, users) = await _authGateClient.GetUsersAsync(cancellationToken);
            if ((int)statusCode >= 200 && (int)statusCode <= 299)
            {
                return Ok(users);
            }

            return StatusCode((int)statusCode, "Failed to fetch users from AuthGate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from AuthGate");
            return StatusCode(500, new { error = "An error occurred while fetching users" });
        }
    }

    /// <summary>
    /// Delete a user by ID (via AuthGate)
    /// </summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = Permissions.TeamManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var statusCode = await _authGateClient.DeleteUserAsync(userId, cancellationToken);

            if ((int)statusCode >= 200 && (int)statusCode <= 299)
            {
                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return NoContent();
            }

            if (statusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "User not found" });
            }

            return StatusCode((int)statusCode, "Failed to delete user from AuthGate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} from AuthGate", userId);
            return StatusCode(500, new { error = "An error occurred while deleting the user" });
        }
    }

    // ========================
    // USER SETTINGS ENDPOINTS
    // ========================

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var query = new GetUserProfileQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get current user preferences
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences()
    {
        var query = new GetPreferencesQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Update current user preferences
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get current user notification settings
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications()
    {
        var query = new GetNotificationsQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Update current user notification settings
    /// </summary>
    [HttpPut("notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationsCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Upload user profile photo
    /// </summary>
    [HttpPost("profile/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Invalid file type. Only images are allowed." });

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds 5MB limit." });

        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Generate URL
            var photoUrl = $"/uploads/profiles/{fileName}";

            // Update profile
            var command = new UpdateUserPhotoCommand { PhotoUrl = photoUrl };
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return StatusCode(500, new { error = "Error uploading photo" });
        }
    }
}

/// <summary>
/// User DTO for listing users
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public string? TenantCode { get; init; }
    public bool IsActive { get; init; }
    public bool MfaEnabled { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
