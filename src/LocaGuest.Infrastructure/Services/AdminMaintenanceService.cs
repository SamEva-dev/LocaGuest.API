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
            if (await _context.EmailDeliveryEvents.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.EmailDeliveryEvents.RemoveRange(_context.EmailDeliveryEvents.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.TrackingEvents.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.TrackingEvents.RemoveRange(_context.TrackingEvents.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.SatisfactionFeedbacks.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.SatisfactionFeedbacks.RemoveRange(_context.SatisfactionFeedbacks.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UsageEvents.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UsageEvents.RemoveRange(_context.UsageEvents.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UsageAggregates.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UsageAggregates.RemoveRange(_context.UsageAggregates.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Subscriptions.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Subscriptions.RemoveRange(_context.Subscriptions.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UserSessions.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UserSessions.RemoveRange(_context.UserSessions.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.NotificationSettings.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.NotificationSettings.RemoveRange(_context.NotificationSettings.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UserPreferences.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UserPreferences.RemoveRange(_context.UserPreferences.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UserProfiles.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UserProfiles.RemoveRange(_context.UserProfiles.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.UserSettings.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.UserSettings.RemoveRange(_context.UserSettings.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.ScenarioShares.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.ScenarioShares.RemoveRange(_context.ScenarioShares.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.ScenarioVersions.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.ScenarioVersions.RemoveRange(_context.ScenarioVersions.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.RentabilityScenarios.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.RentabilityScenarios.RemoveRange(_context.RentabilityScenarios.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.RentInvoiceLines.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.RentInvoiceLines.RemoveRange(_context.RentInvoiceLines.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.RentInvoices.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.RentInvoices.RemoveRange(_context.RentInvoices.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.DepositTransactions.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.DepositTransactions.RemoveRange(_context.DepositTransactions.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Deposits.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Deposits.RemoveRange(_context.Deposits.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.ContractDocumentLinks.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.ContractDocumentLinks.RemoveRange(_context.ContractDocumentLinks.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.ContractParticipants.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.ContractParticipants.RemoveRange(_context.ContractParticipants.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Addendums.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Addendums.RemoveRange(_context.Addendums.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

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

            if (await _context.Documents.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Documents.RemoveRange(_context.Documents.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Contracts.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Contracts.RemoveRange(_context.Contracts.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.PropertyImages.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.PropertyImages.RemoveRange(_context.PropertyImages.IgnoreQueryFilters());
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

            if (await _context.Occupants.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Occupants.RemoveRange(_context.Occupants.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.InvitationTokens.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.InvitationTokens.RemoveRange(_context.InvitationTokens.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.Invitations.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.Invitations.RemoveRange(_context.Invitations.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.TeamMembers.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.TeamMembers.RemoveRange(_context.TeamMembers.IgnoreQueryFilters());
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (await _context.IdempotencyRequests.IgnoreQueryFilters().AnyAsync(cancellationToken))
            {
                _context.IdempotencyRequests.RemoveRange(_context.IdempotencyRequests.IgnoreQueryFilters());
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
                "EmailDeliveryEvents", "TrackingEvents", "SatisfactionFeedbacks",
                "UsageEvents", "UsageAggregates", "Subscriptions",
                "UserSessions", "NotificationSettings", "UserPreferences", "UserProfiles", "UserSettings",
                "ScenarioShares", "ScenarioVersions", "RentabilityScenarios",
                "RentInvoiceLines", "RentInvoices",
                "DepositTransactions", "Deposits",
                "ContractDocumentLinks", "ContractParticipants", "Addendums",
                "InventoryExits", "InventoryEntries", "Payments", "Documents", "Contracts",
                "PropertyImages", "PropertyRooms", "Properties",
                "Occupants",
                "InvitationTokens", "Invitations", "TeamMembers", "IdempotencyRequests", "OrganizationSequences", "Organizations"
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
