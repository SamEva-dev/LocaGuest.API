using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvent;

public record GetTrackingEventQuery(Guid Id) : IRequest<Result<TrackingEventReadDto>>;
