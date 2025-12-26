using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvent;

public class GetTrackingEventQueryHandler : IRequestHandler<GetTrackingEventQuery, Result<TrackingEventReadDto>>
{
    private readonly ILocaGuestDbContext _dbContext;
    private readonly ILogger<GetTrackingEventQueryHandler> _logger;

    public GetTrackingEventQueryHandler(ILocaGuestDbContext dbContext, ILogger<GetTrackingEventQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TrackingEventReadDto>> Handle(GetTrackingEventQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _dbContext.TrackingEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null)
                return Result.Failure<TrackingEventReadDto>($"TrackingEvent with ID {request.Id} not found");

            var dto = new TrackingEventReadDto(
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
                entity.HttpStatusCode);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracking event {TrackingEventId}", request.Id);
            return Result.Failure<TrackingEventReadDto>($"Error getting tracking event: {ex.Message}");
        }
    }
}
