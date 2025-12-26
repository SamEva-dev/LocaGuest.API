using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvents;

public class GetTrackingEventsQueryHandler : IRequestHandler<GetTrackingEventsQuery, Result<PagedResult<TrackingEventReadDto>>>
{
    private readonly ILocaGuestDbContext _dbContext;
    private readonly ILogger<GetTrackingEventsQueryHandler> _logger;

    public GetTrackingEventsQueryHandler(ILocaGuestDbContext dbContext, ILogger<GetTrackingEventsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<TrackingEventReadDto>>> Handle(GetTrackingEventsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 ? 50 : request.PageSize;

            var query = _dbContext.TrackingEvents.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EventType))
                query = query.Where(x => x.EventType == request.EventType);

            if (request.UserId.HasValue)
                query = query.Where(x => x.UserId == request.UserId.Value);

            if (!string.IsNullOrWhiteSpace(request.SessionId))
                query = query.Where(x => x.SessionId == request.SessionId);

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                query = query.Where(x => x.CorrelationId == request.CorrelationId);

            if (request.FromUtc.HasValue)
                query = query.Where(x => x.Timestamp >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.Timestamp <= request.ToUtc.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(entity => new TrackingEventReadDto(
                    entity.Id,
                    entity.TenantId,
                    entity.UserId,
                    entity.EventType,
                    entity.PageName,
                    entity.Url,
                    entity.UserAgent,
                    entity.IpAddress,
                    entity.Timestamp,
                    entity.Metadata,
                    entity.SessionId,
                    entity.CorrelationId,
                    entity.DurationMs,
                    entity.HttpStatusCode))
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<TrackingEventReadDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracking events");
            return Result.Failure<PagedResult<TrackingEventReadDto>>($"Error getting tracking events: {ex.Message}");
        }
    }
}
