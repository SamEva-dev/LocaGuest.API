using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, Result<CheckoutSessionDto>>
{
    private readonly IStripeService _stripeService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

    public CreateCheckoutSessionCommandHandler(
        IStripeService stripeService,
        ITenantContext tenantContext,
        ILogger<CreateCheckoutSessionCommandHandler> logger)
    {
        _stripeService = stripeService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || !_tenantContext.UserId.HasValue)
                return Result.Failure<CheckoutSessionDto>("User not authenticated");

            var userId = _tenantContext.UserId.Value.ToString();

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
