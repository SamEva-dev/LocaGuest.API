using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using LocaGuest.Domain.Audit;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using System.Text.Json;

namespace LocaGuest.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor to automatically capture entity changes for auditing
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _orgContext;
    private readonly IAuditService _auditService;

    public AuditSaveChangesInterceptor(
        ICurrentUserService currentUserService,
        IOrganizationContext orgContext,
        IAuditService auditService)
    {
        _currentUserService = currentUserService;
        _orgContext = orgContext;
        _auditService = auditService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await AuditChangesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AuditChangesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavingChanges(eventData, result);
    }

    private async Task AuditChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        if (!entries.Any())
            return;

        var userId = _currentUserService.UserId;
        var userEmail = _currentUserService.UserEmail;
        var tenantId = _orgContext.OrganizationId;
        var ipAddress = _currentUserService.IpAddress ?? "Unknown";

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry, userId, userEmail, tenantId, ipAddress);
            
            if (auditLog is not null)
            {
                await _auditService.LogEntityChangeAsync(auditLog, cancellationToken);
            }
        }
    }

    private AuditLog? CreateAuditLog(
        EntityEntry entry,
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        string ipAddress)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);
        var action = entry.State switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };

        string? oldValues = null;
        string? newValues = null;
        string? changes = null;

        if (entry.State == EntityState.Modified)
        {
            var modifiedProperties = entry.Properties
                .Where(p => p.IsModified)
                .ToDictionary(
                    p => p.Metadata.Name,
                    p => new { OldValue = p.OriginalValue, NewValue = p.CurrentValue });

            if (modifiedProperties.Any())
            {
                changes = JsonSerializer.Serialize(modifiedProperties, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
            }
        }

        if (entry.State == EntityState.Added)
        {
            var currentValues = entry.Properties
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            
            newValues = JsonSerializer.Serialize(currentValues, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
        }

        if (entry.State == EntityState.Deleted)
        {
            var originalValues = entry.Properties
                .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            
            oldValues = JsonSerializer.Serialize(originalValues, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
        }

        return AuditLog.Create(
            action: action,
            entityType: entityType,
            entityId: entityId,
            userId: userId,
            userEmail: userEmail,
            organizationId: tenantId,
            ipAddress: ipAddress,
            oldValues: oldValues,
            newValues: newValues,
            changes: changes);
    }

    private string? GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(p => p.Metadata.IsKey())
            .ToList();

        if (!keyProperties.Any())
            return null;

        if (keyProperties.Count == 1)
            return keyProperties[0].CurrentValue?.ToString();

        return string.Join(",", keyProperties.Select(p => p.CurrentValue?.ToString() ?? "null"));
    }
}
