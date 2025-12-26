using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Commands.DeleteAuditLog;

public class DeleteAuditLogCommandHandler : IRequestHandler<DeleteAuditLogCommand, Result>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<DeleteAuditLogCommandHandler> _logger;

    public DeleteAuditLogCommandHandler(IAuditDbContext auditDbContext, ILogger<DeleteAuditLogCommandHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteAuditLogCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _auditDbContext.AuditLogs
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
                return Result.Failure($"AuditLog with ID {request.Id} not found");

            _auditDbContext.AuditLogs.Remove(entity);
            await _auditDbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audit log {AuditLogId}", request.Id);
            return Result.Failure($"Error deleting audit log: {ex.Message}");
        }
    }
}
