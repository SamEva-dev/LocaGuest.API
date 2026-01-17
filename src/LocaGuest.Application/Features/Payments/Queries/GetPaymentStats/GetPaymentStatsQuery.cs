using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentStats;

public record GetPaymentStatsQuery : IRequest<Result<PaymentStatsDto>>
{
    public string? OccupantId { get; init; }
    public string? PropertyId { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }
}
