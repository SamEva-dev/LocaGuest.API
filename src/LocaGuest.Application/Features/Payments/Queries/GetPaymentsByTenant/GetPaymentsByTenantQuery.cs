using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsByTenant;

public record GetPaymentsByTenantQuery : IRequest<Result<List<PaymentDto>>>
{
    public required string OccupantId { get; init; }
}
