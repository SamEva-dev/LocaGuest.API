using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<AuditLogDto>>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<GetAuditLogQueryHandler> _logger;

    public GetAuditLogQueryHandler(IAuditDbContext auditDbContext, ILogger<GetAuditLogQueryHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result<AuditLogDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _auditDbContext.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
                return Result.Failure<AuditLogDto>($"AuditLog with ID {request.Id} not found");

            var dto = new AuditLogDto(
                entity.Id,
                entity.UserId,
                entity.UserEmail,
                entity.TenantId,
                entity.Action,
                entity.EntityType,
                entity.EntityId,
                entity.Timestamp,
                entity.IpAddress,
                entity.UserAgent,
                entity.OldValues,
                entity.NewValues,
                entity.Changes,
                entity.RequestPath,
                entity.HttpMethod,
                entity.StatusCode,
                entity.DurationMs,
                entity.CorrelationId,
                entity.SessionId,
                entity.AdditionalData);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log {AuditLogId}", request.Id);
            return Result.Failure<AuditLogDto>($"Error getting audit log: {ex.Message}");
        }
    }
}
