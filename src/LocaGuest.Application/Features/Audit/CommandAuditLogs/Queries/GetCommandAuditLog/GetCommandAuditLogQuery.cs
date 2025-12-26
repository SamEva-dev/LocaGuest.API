using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Audit;
using MediatR;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLog;

public record GetCommandAuditLogQuery(Guid Id) : IRequest<Result<CommandAuditLogDto>>;
