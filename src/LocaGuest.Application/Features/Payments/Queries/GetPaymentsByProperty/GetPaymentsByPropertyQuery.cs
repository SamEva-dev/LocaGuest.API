using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsByProperty;

public record GetPaymentsByPropertyQuery : IRequest<Result<List<PaymentDto>>>
{
    public required string PropertyId { get; init; }
}
