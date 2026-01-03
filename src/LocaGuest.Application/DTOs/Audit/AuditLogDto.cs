namespace LocaGuest.Application.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    Guid? OrganizationId,
    string Action,
    string EntityType,
    string? EntityId,
    DateTime Timestamp,
    string IpAddress,
    string? UserAgent,
    string? OldValues,
    string? NewValues,
    string? Changes,
    string? RequestPath,
    string? HttpMethod,
    int? StatusCode,
    long? DurationMs,
    string? CorrelationId,
    string? SessionId,
    string? AdditionalData
);
