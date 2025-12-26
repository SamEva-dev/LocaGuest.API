using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Commands.DeleteAuditLog;

public record DeleteAuditLogCommand(Guid Id) : IRequest<Result>;
