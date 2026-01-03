using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Queries.GetBillingInvoices;

public class GetBillingInvoicesQueryHandler : IRequestHandler<GetBillingInvoicesQuery, Result<List<BillingInvoiceDto>>>
{
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetBillingInvoicesQueryHandler> _logger;

    public GetBillingInvoicesQueryHandler(
        IStripeService stripeService,
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        ILogger<GetBillingInvoicesQueryHandler> logger)
    {
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<BillingInvoiceDto>>> Handle(GetBillingInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<List<BillingInvoiceDto>>("User not authenticated");

            var userId = _currentUserService.UserId.Value;
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            {
                // Return empty list for users without subscription
                return Result.Success(new List<BillingInvoiceDto>());
            }

            var invoices = await _stripeService.GetInvoicesAsync(subscription.StripeCustomerId, cancellationToken);

            var dtos = invoices.Select(inv => new BillingInvoiceDto(
                inv.Id,
                inv.Created,
                inv.Description ?? "Abonnement mensuel",
                inv.AmountPaid / 100m,
                inv.Currency.ToUpper(),
                inv.Status,
                inv.InvoicePdf
            )).ToList();

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving billing invoices");
            return Result.Failure<List<BillingInvoiceDto>>($"Error retrieving invoices: {ex.Message}");
        }
    }
}
