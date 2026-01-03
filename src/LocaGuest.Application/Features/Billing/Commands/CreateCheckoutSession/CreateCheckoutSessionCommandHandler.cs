using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, Result<CheckoutSessionDto>>
{
    private readonly IStripeService _stripeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

    public CreateCheckoutSessionCommandHandler(
        IStripeService stripeService,
        ICurrentUserService currentUserService,
        ILogger<CreateCheckoutSessionCommandHandler> logger)
    {
        _stripeService = stripeService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<CheckoutSessionDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value.ToString();

            var sessionData = await _stripeService.CreateCheckoutSessionAsync(
                userId,
                request.PlanId,
                request.IsAnnual,
                request.SuccessUrl ?? "",
                request.CancelUrl ?? "",
                cancellationToken
            );

            var dto = new CheckoutSessionDto(sessionData.Id, sessionData.Url);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return Result.Failure<CheckoutSessionDto>($"Error creating checkout session: {ex.Message}");
        }
    }
}
