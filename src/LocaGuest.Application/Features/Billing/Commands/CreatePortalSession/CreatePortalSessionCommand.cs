using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Commands.CreatePortalSession;

public record CreatePortalSessionCommand : IRequest<Result<PortalSessionDto>>
{
    public string? ReturnUrl { get; init; }
}

public record PortalSessionDto(string Url);
