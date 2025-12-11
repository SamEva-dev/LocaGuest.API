using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Commands.CreateCheckoutSession;

public record CreateCheckoutSessionCommand : IRequest<Result<CheckoutSessionDto>>
{
    public string PlanId { get; init; } = string.Empty;
    public bool IsAnnual { get; init; }
    public string? SuccessUrl { get; init; }
    public string? CancelUrl { get; init; }
}

public record CheckoutSessionDto(
    string SessionId,
    string CheckoutUrl
);
