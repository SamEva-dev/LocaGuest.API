using LocaGuest.Domain.Aggregates.UserAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IUserPreferencesRepository : IRepository<UserPreferences>
{
    Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
