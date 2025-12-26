using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Audit;
using MediatR;

namespace LocaGuest.Application.Features.Audit.CommandAuditLogs.Queries.GetCommandAuditLogs;

public record GetCommandAuditLogsQuery : IRequest<Result<PagedResult<CommandAuditLogDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public string? CommandName { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TenantId { get; init; }
    public bool? Success { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
