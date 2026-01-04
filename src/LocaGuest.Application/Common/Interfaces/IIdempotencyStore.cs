namespace LocaGuest.Application.Common.Interfaces;

public record IdempotencyRecord(
    Guid Id,
    string ClientId,
    string IdempotencyKey,
    string RequestHash,
    int StatusCode,
    string ResponseJson,
    string ResponseBodyBase64,
    string ResponseContentType);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string clientId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IdempotencyRecord> CreatePlaceholderAsync(
        string clientId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid id,
        int statusCode,
        string responseContentType,
        string responseBodyBase64,
        string responseJson,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
