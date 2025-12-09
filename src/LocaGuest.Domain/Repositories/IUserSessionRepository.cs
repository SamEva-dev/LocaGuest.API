using LocaGuest.Domain.Aggregates.UserAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IUserSessionRepository : IRepository<UserSession>
{
    Task<List<UserSession>> GetActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
}
