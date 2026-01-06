using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetPropertyPayments;

public record GetPropertyPaymentsQuery : IRequest<Result<List<PaymentDto>>>
{
    public required string PropertyId { get; init; }
}
