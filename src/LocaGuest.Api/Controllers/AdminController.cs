using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocaGuest.Application.Features.Admin.Commands.CleanDatabase;
using LocaGuest.Application.Features.Admin.Queries.GetDatabaseStats;
using MediatR;

namespace LocaGuest.Api.Controllers;

/// <summary>
/// Contrôleur admin pour opérations de maintenance
/// </summary>
//[Authorize(Policy = "SuperAdmin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Nettoyer complètement la base de données (sauf utilisateur connecté)
    /// ATTENTION: Cette action est irréversible!
    /// </summary>
    /// <returns>Confirmation du nettoyage</returns>
    [HttpDelete("clean-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanDatabase()
    {
        //var enabled = string.Equals(_configuration["Admin:EnableDatabaseCleanup"], "true", StringComparison.OrdinalIgnoreCase);
        //if (!enabled)
        //{
        //    return NotFound();
        //}

        try
        {
            _logger.LogWarning("⚠️ CLEAN DATABASE REQUESTED - Deleting all data except connected user");

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "Unable to identify connected user" });
            }

            _logger.LogInformation("Preserving user: {UserId}", userId);

            var result = await _mediator.Send(new CleanDatabaseCommand { PreservedUserId = userId });

            if (!result.IsSuccess)
                return StatusCode(500, new { error = result.ErrorMessage });

            return Ok(new
            {
                message = "Database cleaned successfully",
                preservedUser = result.Data!.PreservedUser,
                deletedTables = result.Data.DeletedTables
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error cleaning database");
            return StatusCode(500, new { error = $"Error cleaning database: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtenir les statistiques de la base de données
    /// </summary>
    /// <returns>Statistiques des tables</returns>
    [HttpGet("database-stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabaseStats()
    {
        var result = await _mediator.Send(new GetDatabaseStatsQuery());

        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.ErrorMessage });

        return Ok(result.Data);
    }
}
