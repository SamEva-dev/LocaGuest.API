using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.EmailDelivery.Commands.HandleBrevoWebhook;

public record HandleBrevoWebhookCommand : IRequest<Result<bool>>
{
    public string RawPayload { get; init; } = string.Empty;
}
