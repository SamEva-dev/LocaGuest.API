using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Commands.CancelSubscription;

public record CancelSubscriptionCommand : IRequest<Result<bool>>
{
    public bool CancelImmediately { get; init; } = false;
}
