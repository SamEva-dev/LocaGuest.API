using LocaGuest.Domain.Aggregates.UserAggregate;

namespace LocaGuest.Domain.Repositories;

public interface INotificationSettingsRepository : IRepository<NotificationSettings>
{
    Task<NotificationSettings?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
