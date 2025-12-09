using LocaGuest.Domain.Aggregates.UserAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IUserProfileRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
