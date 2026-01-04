using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Commands.HandleStripeWebhook;

public record HandleStripeWebhookCommand : IRequest<Result<bool>>
{
    public string Payload { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}
