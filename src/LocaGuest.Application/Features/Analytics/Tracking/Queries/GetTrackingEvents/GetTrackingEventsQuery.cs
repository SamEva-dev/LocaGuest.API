using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvents;

public record GetTrackingEventsQuery : IRequest<Result<PagedResult<TrackingEventReadDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public string? EventType { get; init; }
    public Guid? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
