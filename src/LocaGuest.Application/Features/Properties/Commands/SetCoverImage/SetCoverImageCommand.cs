using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.SetCoverImage;

public record SetCoverImageCommand : IRequest<Result>
{
    public Guid PropertyId { get; init; }
    public Guid ImageId { get; init; }
}
