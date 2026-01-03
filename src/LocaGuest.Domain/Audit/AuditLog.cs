namespace LocaGuest.Domain.Audit;

/// <summary>
/// Audit log entry for tracking all system changes and actions
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    
    // Who
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public Guid? OrganizationId { get; private set; }
    
    // What
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    
    // When
    public DateTime Timestamp { get; private set; }
    
    // Where
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    
    // How (Details)
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? Changes { get; private set; }
    
    // Context
    public string? RequestPath { get; private set; }
    public string? HttpMethod { get; private set; }
    public int? StatusCode { get; private set; }
    public long? DurationMs { get; private set; }
    
    // Additional metadata
    public string? CorrelationId { get; private set; }
    public string? SessionId { get; private set; }
    public string? AdditionalData { get; private set; }
    
    private AuditLog() { }
    
    public static AuditLog Create(
        string action,
        string entityType,
        string? entityId,
        Guid? userId,
        string? userEmail,
        Guid? organizationId,
        string ipAddress,
        string? userAgent = null,
        string? oldValues = null,
        string? newValues = null,
        string? changes = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? correlationId = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserEmail = userEmail,
            OrganizationId = organizationId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OldValues = oldValues,
            NewValues = newValues,
            Changes = changes,
            RequestPath = requestPath,
            HttpMethod = httpMethod,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void SetRequestDetails(string? path, string? method, int? statusCode, long? durationMs)
    {
        RequestPath = path;
        HttpMethod = method;
        StatusCode = statusCode;
        DurationMs = durationMs;
    }
    
    public void SetSessionInfo(string? sessionId, string? additionalData = null)
    {
        SessionId = sessionId;
        AdditionalData = additionalData;
    }
}
