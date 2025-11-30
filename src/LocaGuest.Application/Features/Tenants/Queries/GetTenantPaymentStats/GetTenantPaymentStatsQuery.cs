using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenantPaymentStats;

public record GetTenantPaymentStatsQuery(Guid TenantId) : IRequest<Result<TenantPaymentStatsDto>>;

public record TenantPaymentStatsDto
{
    public Guid TenantId { get; init; }
    public decimal TotalPaid { get; init; }
    public int TotalPayments { get; init; }
    public int LatePayments { get; init; }
    public decimal OnTimeRate { get; init; }
}
