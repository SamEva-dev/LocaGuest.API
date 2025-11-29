using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.RecordPayment;

public record RecordPaymentCommand : IRequest<Result<Guid>>
{
    public Guid ContractId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PaymentDate { get; init; }
    public string Method { get; init; } = string.Empty;
    public string? Reference { get; init; }
}
