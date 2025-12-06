using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Commands.CreatePayment;

public record CreatePaymentCommand : IRequest<Result<PaymentDto>>
{
    public Guid TenantId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid ContractId { get; init; }
    public decimal AmountDue { get; init; }
    public decimal AmountPaid { get; init; }
    public DateTime? PaymentDate { get; init; }
    public DateTime ExpectedDate { get; init; }
    public string PaymentMethod { get; init; } = "BankTransfer";
    public string? Note { get; init; }
}
