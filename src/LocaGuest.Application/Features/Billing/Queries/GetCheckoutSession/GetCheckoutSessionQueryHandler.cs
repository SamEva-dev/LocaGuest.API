using LocaGuest.Application.Common;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Queries.GetCheckoutSession;

public class GetCheckoutSessionQueryHandler : IRequestHandler<GetCheckoutSessionQuery, Result<CheckoutSessionStatusDto>>
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<GetCheckoutSessionQueryHandler> _logger;

    public GetCheckoutSessionQueryHandler(IStripeService stripeService, ILogger<GetCheckoutSessionQueryHandler> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<Result<CheckoutSessionStatusDto>> Handle(GetCheckoutSessionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _stripeService.GetCheckoutSessionAsync(request.SessionId, cancellationToken);
            return Result.Success(new CheckoutSessionStatusDto(session.Status, session.CustomerEmail, session.SubscriptionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving checkout session {SessionId}", request.SessionId);
            return Result.Failure<CheckoutSessionStatusDto>($"Error retrieving session: {ex.Message}");
        }
    }
}
