using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Subscriptions.Queries.CheckFeature;

public class CheckFeatureQueryHandler : IRequestHandler<CheckFeatureQuery, Result<object>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<CheckFeatureQueryHandler> _logger;

    public CheckFeatureQueryHandler(
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<CheckFeatureQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(CheckFeatureQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<object>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var hasAccess = await _subscriptionService.CanAccessFeatureAsync(userId, request.FeatureName, cancellationToken);

            return Result.Success<object>(new { feature = request.FeatureName, hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature {Feature}", request.FeatureName);
            return Result.Failure<object>("Error checking feature");
        }
    }
}
