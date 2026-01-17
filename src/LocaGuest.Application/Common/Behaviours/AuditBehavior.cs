using MediatR;
using Microsoft.Extensions.Logging;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Audit;
using System.Diagnostics;
using System.Text.Json;

namespace LocaGuest.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior to audit all commands
/// </summary>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _orgContext;
    private readonly IAuditService _auditService;

    public AuditBehavior(
        ILogger<AuditBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        IOrganizationContext orgContext,
        IAuditService auditService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _orgContext = orgContext;
        _auditService = auditService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // Only audit commands (not queries)
        if (!requestName.EndsWith("Command"))
        {
            return await next();
        }

        var userId = _currentUserService.UserId;
        var userEmail = _currentUserService.UserEmail;
        var organizationId = _orgContext.OrganizationId;
        var ipAddress = _currentUserService.IpAddress ?? "Unknown";

        // Serialize command data (excluding sensitive fields)
        var commandData = SerializeCommand(request);

        var auditLog = CommandAuditLog.Create(
            commandName: requestName,
            commandData: commandData,
            userId: userId,
            userEmail: userEmail,
            organizationId: organizationId,
            ipAddress: ipAddress);

        var stopwatch = Stopwatch.StartNew();
        TResponse response;

        try
        {
            _logger.LogInformation(
                "Executing command {CommandName} for user {UserId} (Tenant: {OccupantId})",
                requestName, userId, organizationId);

            response = await next();

            stopwatch.Stop();

            // Serialize result (excluding sensitive data)
            var resultData = SerializeResult(response);
            auditLog.MarkAsCompleted(stopwatch.ElapsedMilliseconds, resultData);

            _logger.LogInformation(
                "Command {CommandName} completed successfully in {Duration}ms",
                requestName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            auditLog.MarkAsFailed(
                stopwatch.ElapsedMilliseconds,
                ex.Message,
                ex.StackTrace);

            _logger.LogError(ex,
                "Command {CommandName} failed after {Duration}ms for user {UserId}",
                requestName, stopwatch.ElapsedMilliseconds, userId);

            // Re-throw to maintain exception flow
            throw;
        }
        finally
        {
            // Save audit log to dedicated database
            await _auditService.LogCommandAsync(auditLog, cancellationToken);
        }

        return response;
    }

    private string SerializeCommand(TRequest request)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(request, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize command {CommandName}", typeof(TRequest).Name);
            return $"{{\"error\": \"Serialization failed: {ex.Message}\"}}";
        }
    }

    private string? SerializeResult(TResponse response)
    {
        try
        {
            // Don't serialize large responses
            if (response is IEnumerable<object> collection && collection.Count() > 100)
            {
                return $"{{\"count\": {collection.Count()}}}";
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(response, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize response for {CommandName}", typeof(TRequest).Name);
            return null;
        }
    }
}
