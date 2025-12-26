using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<GetAuditLogsQueryHandler> _logger;

    public GetAuditLogsQueryHandler(IAuditDbContext auditDbContext, ILogger<GetAuditLogsQueryHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 ? 50 : request.PageSize;

            var query = _auditDbContext.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(x => x.Action == request.Action);

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(x => x.EntityType == request.EntityType);

            if (!string.IsNullOrWhiteSpace(request.EntityId))
                query = query.Where(x => x.EntityId == request.EntityId);

            if (request.UserId.HasValue)
                query = query.Where(x => x.UserId == request.UserId);

            if (request.TenantId.HasValue)
                query = query.Where(x => x.TenantId == request.TenantId);

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                query = query.Where(x => x.CorrelationId == request.CorrelationId);

            if (request.FromUtc.HasValue)
                query = query.Where(x => x.Timestamp >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.Timestamp <= request.ToUtc.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(entity => new AuditLogDto(
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
                    entity.AdditionalData))
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return Result.Failure<PagedResult<AuditLogDto>>($"Error getting audit logs: {ex.Message}");
        }
    }
}
