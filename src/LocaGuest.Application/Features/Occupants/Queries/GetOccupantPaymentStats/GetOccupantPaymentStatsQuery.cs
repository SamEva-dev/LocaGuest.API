using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantPaymentStats;

public record GetOccupantPaymentStatsQuery(Guid OccupantId) : IRequest<Result<OccupantPaymentStatsDto>>;

public record OccupantPaymentStatsDto
{
    public Guid OccupantId { get; init; }
    public decimal TotalPaid { get; init; }
    public int TotalPayments { get; init; }
    public int LatePayments { get; init; }
    public decimal OnTimeRate { get; init; }
}
