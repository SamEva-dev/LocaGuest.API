using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Commands.DeleteCommandAuditLog;

public record DeleteCommandAuditLogCommand(Guid Id) : IRequest<Result>;
