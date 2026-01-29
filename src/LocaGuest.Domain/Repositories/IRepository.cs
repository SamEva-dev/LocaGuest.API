using System.Linq.Expressions;

namespace LocaGuest.Domain.Repositories;

/// <summary>
/// Generic repository interface following DDD principles
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    IQueryable<T> Query(bool asNoTracking = false);
}
