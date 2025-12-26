using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Commands.DeleteCommandAuditLog;

public class DeleteCommandAuditLogCommandHandler : IRequestHandler<DeleteCommandAuditLogCommand, Result>
{
    private readonly IAuditDbContext _auditDbContext;
    private readonly ILogger<DeleteCommandAuditLogCommandHandler> _logger;

    public DeleteCommandAuditLogCommandHandler(IAuditDbContext auditDbContext, ILogger<DeleteCommandAuditLogCommandHandler> logger)
    {
        _auditDbContext = auditDbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteCommandAuditLogCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _auditDbContext.CommandAuditLogs
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
                return Result.Failure($"CommandAuditLog with ID {request.Id} not found");

            _auditDbContext.CommandAuditLogs.Remove(entity);
            await _auditDbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting command audit log {CommandAuditLogId}", request.Id);
            return Result.Failure($"Error deleting command audit log: {ex.Message}");
        }
    }
}
