using LocaGuest.Application.Common;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Result<bool>>
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(IStripeService stripeService, ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _stripeService.HandleWebhookEventAsync(request.Payload, request.Signature, cancellationToken);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing error");
            return Result.Failure<bool>("Stripe webhook error");
        }
    }
}
