using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

/// <summary>
/// Contrôleur admin pour opérations de maintenance
/// </summary>
[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly AuditDbContext _auditContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LocaGuestDbContext context,
        AuditDbContext auditContext,
        ILogger<AdminController> logger)
    {
        _context = context;
        _auditContext = auditContext;
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
        try
        {
            _logger.LogWarning("⚠️ CLEAN DATABASE REQUESTED - Deleting all data except connected user");

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "Unable to identify connected user" });
            }

            _logger.LogInformation("Preserving user: {UserId}", userId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Utiliser EF Core pour supprimer (évite les erreurs SQL de noms de tables)
                
                // 1. Inventory Exits & Entries
                if (_context.InventoryExits.Any())
                {
                    _context.InventoryExits.RemoveRange(_context.InventoryExits);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Inventory Exits deleted");
                }
                
                if (_context.InventoryEntries.Any())
                {
                    _context.InventoryEntries.RemoveRange(_context.InventoryEntries);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Inventory Entries deleted");
                }

                // 2. Payments
                if (_context.Payments.Any())
                {
                    _context.Payments.RemoveRange(_context.Payments);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Payments deleted");
                }

                // 3. Contracts
                if (_context.Contracts.Any())
                {
                    _context.Contracts.RemoveRange(_context.Contracts);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Contracts deleted");
                }

                // 3. Invoiceq
                if (_context.RentInvoices.Any())
                {
                    _context.RentInvoices.RemoveRange(_context.RentInvoices);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Rent Invoices deleted");
                }

                // 4. Documents
                if (_context.Documents.Any())
                {
                    _context.Documents.RemoveRange(_context.Documents);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Documents deleted");
                }

                // 5. Properties (avec Rooms)
                if (_context.PropertyRooms.Any())
                {
                    _context.PropertyRooms.RemoveRange(_context.PropertyRooms);
                    await _context.SaveChangesAsync();
                }
                if (_context.Properties.Any())
                {
                    _context.Properties.RemoveRange(_context.Properties);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Properties deleted");
                }

                if (_context.PropertyImages.Any())
                {
                    _context.PropertyImages.RemoveRange(_context.PropertyImages);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Images deleted");
                }

                // 6. Tenants
                if (_context.Tenants.Any())
                {
                    _context.Tenants.RemoveRange(_context.Tenants);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Tenants deleted");
                }

                // 7. Rentability Scenarios
                if (_context.RentabilityScenarios.Any())
                {
                    _context.RentabilityScenarios.RemoveRange(_context.RentabilityScenarios);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Rentability scenarios deleted");
                }

                // 8. Organization / tenant root data
                if (_context.InvitationTokens.Any())
                {
                    _context.InvitationTokens.RemoveRange(_context.InvitationTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Invitation tokens deleted");
                }

                if (_context.TeamMembers.Any())
                {
                    _context.TeamMembers.RemoveRange(_context.TeamMembers);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Team members deleted");
                }

                if (_context.OrganizationSequences.Any())
                {
                    _context.OrganizationSequences.RemoveRange(_context.OrganizationSequences);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Tenant sequences deleted");
                }

                if (_context.Organizations.Any())
                {
                    _context.Organizations.RemoveRange(_context.Organizations);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Organizations deleted");
                }
                // 9. User settings (optionnel)
                // await _context.Database.ExecuteSqlRawAsync($"DELETE FROM UserSettings WHERE UserId != '{userId}'");

                await transaction.CommitAsync();
                
                _logger.LogWarning("🧹 Database cleaned successfully - All data deleted except user {UserId}", userId);
                
                return Ok(new 
                { 
                    message = "Database cleaned successfully",
                    preservedUser = userId,
                    deletedTables = new[] 
                    { 
                        "InventoryExits", "InventoryEntries", "Payments", "Contracts", 
                        "Documents", "Properties", "PropertyRooms", "PropertyImages", "Tenants", "RentabilityScenarios",
                        "InvitationTokens", "TeamMembers", "TenantSequences", "Organizations", "RentInvoices"
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
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
        try
        {
            var tenantId = User.FindFirst("organization_id")?.Value ?? User.FindFirst("organizationId")?.Value;
            var userId = User.FindFirst("sub")?.Value
                         ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var stats = new
            {
                Auth = new
                {
                    UserId = userId,
                    TenantId = tenantId
                },

                Organizations = new
                {
                    Raw = await _context.Organizations.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Organizations.CountAsync()
                },
                Properties = new
                {
                    Raw = await _context.Properties.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Properties.CountAsync()
                },
                Tenants = new
                {
                    Raw = await _context.Tenants.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Tenants.CountAsync()
                },
                Contracts = new
                {
                    Raw = await _context.Contracts.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Contracts.CountAsync()
                },
                Payments = new
                {
                    Raw = await _context.Payments.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Payments.CountAsync()
                },
                Documents = new
                {
                    Raw = await _context.Documents.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.Documents.CountAsync()
                },
                InventoryEntries = new
                {
                    Raw = await _context.InventoryEntries.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.InventoryEntries.CountAsync()
                },
                InventoryExits = new
                {
                    Raw = await _context.InventoryExits.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.InventoryExits.CountAsync()
                },
                RentabilityScenarios = new
                {
                    Raw = await _context.RentabilityScenarios.IgnoreQueryFilters().CountAsync(),
                    Filtered = await _context.RentabilityScenarios.CountAsync()
                },
                
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
