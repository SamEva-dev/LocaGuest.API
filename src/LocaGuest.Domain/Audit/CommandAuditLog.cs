namespace LocaGuest.Domain.Audit;

/// <summary>
/// Audit log specific to CQRS commands
/// </summary>
public class CommandAuditLog
{
    public Guid Id { get; private set; }
    
    // Command info
    public string CommandName { get; private set; } = string.Empty;
    public string CommandData { get; private set; } = string.Empty;
    
    // User context
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public Guid? TenantId { get; private set; }
    
    // Execution
    public DateTime ExecutedAt { get; private set; }
    public long DurationMs { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? StackTrace { get; private set; }
    
    // Result
    public string? ResultData { get; private set; }
    
    // Context
    public string IpAddress { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public string? RequestPath { get; private set; }
    
    private CommandAuditLog() { }
    
    public static CommandAuditLog Create(
        string commandName,
        string commandData,
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        string ipAddress,
        string? correlationId = null,
        string? requestPath = null)
    {
        return new CommandAuditLog
        {
            Id = Guid.NewGuid(),
            CommandName = commandName,
            CommandData = commandData,
            UserId = userId,
            UserEmail = userEmail,
            TenantId = tenantId,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            RequestPath = requestPath,
            ExecutedAt = DateTime.UtcNow,
            Success = true
        };
    }
    
    public void MarkAsCompleted(long durationMs, string? resultData = null)
    {
        Success = true;
        DurationMs = durationMs;
        ResultData = resultData;
    }
    
    public void MarkAsFailed(long durationMs, string errorMessage, string? stackTrace = null)
    {
        Success = false;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;
    }
}
