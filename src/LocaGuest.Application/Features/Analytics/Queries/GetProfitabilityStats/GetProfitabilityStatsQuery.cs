using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Queries.GetProfitabilityStats;

public record GetProfitabilityStatsQuery : IRequest<Result<ProfitabilityStatsDto>>
{
}
