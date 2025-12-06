using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Commands.UpdatePayment;

public record UpdatePaymentCommand : IRequest<Result<PaymentDto>>
{
    public required string PaymentId { get; init; }
    public decimal AmountPaid { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethod { get; init; }
    public string? Note { get; init; }
}
