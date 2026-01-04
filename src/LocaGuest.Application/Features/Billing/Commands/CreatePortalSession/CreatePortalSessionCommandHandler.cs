using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.CreatePortalSession;

public class CreatePortalSessionCommandHandler : IRequestHandler<CreatePortalSessionCommand, Result<PortalSessionDto>>
{
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePortalSessionCommandHandler> _logger;

    public CreatePortalSessionCommandHandler(
        IStripeService stripeService,
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        ILogger<CreatePortalSessionCommandHandler> logger)
    {
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PortalSessionDto>> Handle(CreatePortalSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<PortalSessionDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null || string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
                return Result.Failure<PortalSessionDto>("No active subscription found");

            var url = await _stripeService.CreatePortalSessionAsync(
                subscription.StripeCustomerId,
                request.ReturnUrl ?? string.Empty,
                cancellationToken);

            return Result.Success(new PortalSessionDto(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal session");
            return Result.Failure<PortalSessionDto>($"Error creating billing portal session: {ex.Message}");
        }
    }
}
