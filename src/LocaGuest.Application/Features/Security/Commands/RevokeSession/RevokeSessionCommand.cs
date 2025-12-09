using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Security.Commands.RevokeSession;

public record RevokeSessionCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; init; }
}
