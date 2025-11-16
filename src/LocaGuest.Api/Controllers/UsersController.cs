using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;

    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all users (from AuthGate, via HttpClient call)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromServices] IHttpClientFactory httpClientFactory)
    {
        try
        {
            var client = httpClientFactory.CreateClient("AuthGateApi");
            var response = await client.GetAsync("/api/users");

            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
                return Ok(users);
            }

            return StatusCode((int)response.StatusCode, "Failed to fetch users from AuthGate");
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        Guid userId,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        try
        {
            var client = httpClientFactory.CreateClient("AuthGateApi");
            var response = await client.DeleteAsync($"/api/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return NoContent();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "User not found" });
            }

            return StatusCode((int)response.StatusCode, "Failed to delete user from AuthGate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} from AuthGate", userId);
            return StatusCode(500, new { error = "An error occurred while deleting the user" });
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
