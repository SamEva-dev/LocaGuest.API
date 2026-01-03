using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Audit;
using MediatR;

namespace LocaGuest.Application.Features.Audit.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery : IRequest<Result<PagedResult<AuditLogDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
