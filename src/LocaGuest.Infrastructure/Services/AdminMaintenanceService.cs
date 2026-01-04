using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Services;

public class AdminMaintenanceService : IAdminMaintenanceService
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<AdminMaintenanceService> _logger;

    public AdminMaintenanceService(LocaGuestDbContext context, ILogger<AdminMaintenanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CleanDatabaseResult> CleanDatabaseAsync(string preservedUserId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (await _context.InventoryExits.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.InventoryExits.RemoveRange(_context.InventoryExits.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.InventoryEntries.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.InventoryEntries.RemoveRange(_context.InventoryEntries.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Payments.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Payments.RemoveRange(_context.Payments.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Contracts.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Contracts.RemoveRange(_context.Contracts.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.RentInvoices.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.RentInvoices.RemoveRange(_context.RentInvoices.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Documents.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Documents.RemoveRange(_context.Documents.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.PropertyRooms.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.PropertyRooms.RemoveRange(_context.PropertyRooms.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Properties.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Properties.RemoveRange(_context.Properties.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.PropertyImages.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.PropertyImages.RemoveRange(_context.PropertyImages.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Occupants.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Occupants.RemoveRange(_context.Occupants.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.RentabilityScenarios.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.RentabilityScenarios.RemoveRange(_context.RentabilityScenarios.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.InvitationTokens.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.InvitationTokens.RemoveRange(_context.InvitationTokens.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.TeamMembers.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.TeamMembers.RemoveRange(_context.TeamMembers.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.OrganizationSequences.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.OrganizationSequences.RemoveRange(_context.OrganizationSequences.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Organizations.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Organizations.RemoveRange(_context.Organizations.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var deletedTables = new[]
            {
                "InventoryExits", "InventoryEntries", "Payments", "Contracts",
                "Documents", "Properties", "PropertyRooms", "PropertyImages", "Occupants", "RentabilityScenarios",
                "InvitationTokens", "TeamMembers", "OccupantSequences", "Organizations", "RentInvoices"
            };

            _logger.LogWarning("Database cleaned successfully - All data deleted except user {UserId}", preservedUserId);

            return new CleanDatabaseResult(preservedUserId, deletedTables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning database");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
