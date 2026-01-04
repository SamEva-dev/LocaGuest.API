using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetCurrentSubscription;

public record GetCurrentSubscriptionQuery : IRequest<Result<object>>;
