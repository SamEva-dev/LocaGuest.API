namespace LocaGuest.Application.Common.Interfaces;

public record CleanDatabaseResult(string PreservedUser, string[] DeletedTables);

public interface IAdminMaintenanceService
{
    Task<CleanDatabaseResult> CleanDatabaseAsync(string preservedUserId, CancellationToken cancellationToken = default);
}
