using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Audit;
using MediatR;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Queries.GetAuditLog;

public record GetAuditLogQuery(Guid Id) : IRequest<Result<AuditLogDto>>;
