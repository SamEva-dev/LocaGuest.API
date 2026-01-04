using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Subscriptions.Queries.CheckQuota;

public class CheckQuotaQueryHandler : IRequestHandler<CheckQuotaQuery, Result<object>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<CheckQuotaQueryHandler> _logger;

    public CheckQuotaQueryHandler(
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<CheckQuotaQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(CheckQuotaQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<object>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var hasQuota = await _subscriptionService.CheckQuotaAsync(userId, request.Dimension, cancellationToken);
            var currentUsage = await _subscriptionService.GetUsageAsync(userId, request.Dimension, cancellationToken);

            return Result.Success<object>(new
            {
                dimension = request.Dimension,
                hasQuota,
                currentUsage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quota {Dimension}", request.Dimension);
            return Result.Failure<object>("Error checking quota");
        }
    }
}
