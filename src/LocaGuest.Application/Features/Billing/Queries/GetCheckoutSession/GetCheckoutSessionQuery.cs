using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Queries.GetCheckoutSession;

public record GetCheckoutSessionQuery : IRequest<Result<CheckoutSessionStatusDto>>
{
    public string SessionId { get; init; } = string.Empty;
}

public record CheckoutSessionStatusDto(
    string? Status,
    string? CustomerEmail,
    string? SubscriptionId);
