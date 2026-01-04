using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetUsage;

public class GetUsageQueryHandler : IRequestHandler<GetUsageQuery, Result<object>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GetUsageQueryHandler> _logger;

    public GetUsageQueryHandler(
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<GetUsageQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(GetUsageQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<object>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var plan = await _subscriptionService.GetPlanAsync(userId, cancellationToken);

            var usage = new
            {
                scenarios = new
                {
                    current = await _subscriptionService.GetUsageAsync(userId, "scenarios", cancellationToken),
                    limit = plan.MaxScenarios,
                    unlimited = plan.MaxScenarios == int.MaxValue
                },
                exports = new
                {
                    current = await _subscriptionService.GetUsageAsync(userId, "exports", cancellationToken),
                    limit = plan.HasUnlimitedExports ? int.MaxValue : plan.MaxExportsPerMonth,
                    unlimited = plan.HasUnlimitedExports
                },
                aiSuggestions = new
                {
                    current = await _subscriptionService.GetUsageAsync(userId, "ai_suggestions", cancellationToken),
                    limit = plan.HasUnlimitedAi ? int.MaxValue : plan.MaxAiSuggestionsPerMonth,
                    unlimited = plan.HasUnlimitedAi
                },
                shares = new
                {
                    current = await _subscriptionService.GetUsageAsync(userId, "shares", cancellationToken),
                    limit = plan.MaxShares,
                    unlimited = plan.MaxShares == int.MaxValue
                }
            };

            return Result.Success<object>(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage");
            return Result.Failure<object>("Error retrieving usage");
        }
    }
}
