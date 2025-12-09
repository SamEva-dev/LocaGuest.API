using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class UserPreferencesRepository : Repository<UserPreferences>, IUserPreferencesRepository
{
    public UserPreferencesRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
