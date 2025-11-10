using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Queries.GetRevenueEvolution;

public record GetRevenueEvolutionQuery : IRequest<Result<List<RevenueEvolutionDto>>>
{
    public int Months { get; init; } = 6;
    public int? Year { get; init; }
}
