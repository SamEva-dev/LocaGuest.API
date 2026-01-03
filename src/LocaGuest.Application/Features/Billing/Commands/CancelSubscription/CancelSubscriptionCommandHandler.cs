using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result<bool>>
{
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IStripeService stripeService,
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<bool>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
                return Result.Failure<bool>("No active subscription found");

            await _stripeService.CancelSubscriptionAsync(
                subscription.StripeSubscriptionId,
                request.CancelImmediately,
                cancellationToken
            );

            _logger.LogInformation("Subscription {SubscriptionId} canceled for user {UserId}", 
                subscription.Id, userId);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription");
            return Result.Failure<bool>($"Error canceling subscription: {ex.Message}");
        }
    }
}
