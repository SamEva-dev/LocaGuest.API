using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using MediatR;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetActivePlans;

public record GetActivePlansQuery : IRequest<Result<List<Plan>>>;
