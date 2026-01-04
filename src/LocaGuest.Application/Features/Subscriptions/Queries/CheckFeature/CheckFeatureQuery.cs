using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Subscriptions.Queries.CheckFeature;

public record CheckFeatureQuery(string FeatureName) : IRequest<Result<object>>;
