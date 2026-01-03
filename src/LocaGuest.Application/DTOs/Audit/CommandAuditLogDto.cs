namespace LocaGuest.Application.DTOs.Audit;

public record CommandAuditLogDto(
    Guid Id,
    string CommandName,
    string CommandData,
    Guid? UserId,
    string? UserEmail,
    Guid? OrganizationId,
    DateTime ExecutedAt,
    long DurationMs,
    bool Success,
    string? ErrorMessage,
    string? StackTrace,
    string? ResultData,
    string IpAddress,
    string? CorrelationId,
    string? RequestPath
);
