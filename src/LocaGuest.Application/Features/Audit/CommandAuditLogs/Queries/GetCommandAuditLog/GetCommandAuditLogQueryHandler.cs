using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLog;

public class GetCommandAuditLogQueryHandler : IRequestHandler<GetCommandAuditLogQuery, Result<CommandAuditLogDto>>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<GetCommandAuditLogQueryHandler> _logger;

    public GetCommandAuditLogQueryHandler(IAuditDbContext auditDbContext, ILogger<GetCommandAuditLogQueryHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result<CommandAuditLogDto>> Handle(GetCommandAuditLogQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _auditDbContext.CommandAuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
                return Result.Failure<CommandAuditLogDto>($"CommandAuditLog with ID {request.Id} not found");

            var dto = new CommandAuditLogDto(
                entity.Id,
                entity.CommandName,
                entity.CommandData,
                entity.UserId,
                entity.UserEmail,
                entity.OrganizationId,
                entity.ExecutedAt,
                entity.DurationMs,
                entity.Success,
                entity.ErrorMessage,
                entity.StackTrace,
                entity.ResultData,
                entity.IpAddress,
                entity.CorrelationId,
                entity.RequestPath);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting command audit log {CommandAuditLogId}", request.Id);
            return Result.Failure<CommandAuditLogDto>($"Error getting command audit log: {ex.Message}");
        }
    }
}
