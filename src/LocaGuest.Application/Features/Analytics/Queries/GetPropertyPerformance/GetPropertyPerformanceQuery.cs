using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Queries.GetPropertyPerformance;

public record GetPropertyPerformanceQuery : IRequest<Result<List<PropertyPerformanceDto>>>
{
}
