using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Services;

public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly LocaGuestDbContext _db;

    public EfIdempotencyStore(LocaGuestDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyRecord?> GetAsync(string clientId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var entity = await _db.IdempotencyRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IdempotencyKey == idempotencyKey, cancellationToken);

        return entity == null ? null : Map(entity);
    }

    public async Task<IdempotencyRecord> CreatePlaceholderAsync(
        string clientId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var entity = new IdempotencyRequestEntity
        {
            ClientId = clientId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            StatusCode = 0,
            ResponseJson = string.Empty,
            ResponseBodyBase64 = string.Empty,
            ResponseContentType = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.MinValue
        };

        _db.IdempotencyRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task CompleteAsync(
        Guid id,
        int statusCode,
        string responseContentType,
        string responseBodyBase64,
        string responseJson,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.IdempotencyRequests
            .FirstAsync(x => x.Id == id, cancellationToken);

        entity.StatusCode = statusCode;
        entity.ResponseContentType = responseContentType;
        entity.ResponseBodyBase64 = responseBodyBase64;
        entity.ResponseJson = responseJson;
        entity.CompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.IdempotencyRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return;

        _db.IdempotencyRequests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IdempotencyRecord Map(IdempotencyRequestEntity entity)
        => new(
            entity.Id,
            entity.ClientId,
            entity.IdempotencyKey,
            entity.RequestHash,
            entity.StatusCode,
            entity.ResponseJson,
            entity.ResponseBodyBase64,
            entity.ResponseContentType);
}
