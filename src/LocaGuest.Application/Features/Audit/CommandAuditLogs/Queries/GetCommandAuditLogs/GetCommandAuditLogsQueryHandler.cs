using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLogs;

public class GetCommandAuditLogsQueryHandler : IRequestHandler<GetCommandAuditLogsQuery, Result<PagedResult<CommandAuditLogDto>>>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<GetCommandAuditLogsQueryHandler> _logger;

    public GetCommandAuditLogsQueryHandler(IAuditDbContext auditDbContext, ILogger<GetCommandAuditLogsQueryHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<CommandAuditLogDto>>> Handle(GetCommandAuditLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 ? 50 : request.PageSize;

            var query = _auditDbContext.CommandAuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.CommandName))
                query = query.Where(x => x.CommandName == request.CommandName);

            if (request.UserId.HasValue)
                query = query.Where(x => x.UserId == request.UserId);

            if (request.TenantId.HasValue)
                query = query.Where(x => x.TenantId == request.TenantId);

            if (request.Success.HasValue)
                query = query.Where(x => x.Success == request.Success.Value);

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                query = query.Where(x => x.CorrelationId == request.CorrelationId);

            if (request.FromUtc.HasValue)
                query = query.Where(x => x.ExecutedAt >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.ExecutedAt <= request.ToUtc.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.ExecutedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(entity => new CommandAuditLogDto(
                    entity.Id,
                    entity.CommandName,
                    entity.CommandData,
                    entity.UserId,
                    entity.UserEmail,
                    entity.TenantId,
                    entity.ExecutedAt,
                    entity.DurationMs,
                    entity.Success,
                    entity.ErrorMessage,
                    entity.StackTrace,
                    entity.ResultData,
                    entity.IpAddress,
                    entity.CorrelationId,
                    entity.RequestPath))
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<CommandAuditLogDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting command audit logs");
            return Result.Failure<PagedResult<CommandAuditLogDto>>($"Error getting command audit logs: {ex.Message}");
        }
    }
}
