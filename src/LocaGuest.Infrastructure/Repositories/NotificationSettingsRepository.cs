using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class NotificationSettingsRepository : Repository<NotificationSettings>, INotificationSettingsRepository
{
    public NotificationSettingsRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<NotificationSettings?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }
}
