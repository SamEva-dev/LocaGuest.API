using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Commands.VoidPayment;

public record VoidPaymentCommand : IRequest<Result<bool>>
{
    public Guid PaymentId { get; init; }
    public string? Reason { get; init; }
}
