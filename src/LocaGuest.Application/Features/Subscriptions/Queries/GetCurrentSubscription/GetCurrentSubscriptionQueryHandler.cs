using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetCurrentSubscription;

public class GetCurrentSubscriptionQueryHandler : IRequestHandler<GetCurrentSubscriptionQuery, Result<object>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GetCurrentSubscriptionQueryHandler> _logger;

    public GetCurrentSubscriptionQueryHandler(
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<GetCurrentSubscriptionQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<object>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
            {
                var freePlan = await _subscriptionService.GetPlanAsync(userId, cancellationToken);
                return Result.Success<object>(new
                {
                    plan = freePlan,
                    status = "free",
                    isActive = true
                });
            }

            return Result.Success<object>(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription");
            return Result.Failure<object>("Error retrieving current subscription");
        }
    }
}
