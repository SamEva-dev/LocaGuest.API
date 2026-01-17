namespace LocaGuest.Application.DTOs.Analytics;

public record TrackingEventReadDto(
    Guid Id,
    Guid OccupantId,
    Guid UserId,
    string EventType,
    string? PageName,
    string? Url,
    string UserAgent,
    string IpAddress,
    DateTime Timestamp,
    string? Metadata,
    string? SessionId,
    string? CorrelationId,
    int? DurationMs,
    int? HttpStatusCode
);
