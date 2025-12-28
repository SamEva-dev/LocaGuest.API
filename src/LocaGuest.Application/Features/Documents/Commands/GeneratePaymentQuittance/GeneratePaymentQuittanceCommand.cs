using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Commands.GeneratePaymentQuittance;

public record GeneratePaymentQuittanceCommand : IRequest<Result<Guid>>
{
    public required Guid PaymentId { get; init; }
}
