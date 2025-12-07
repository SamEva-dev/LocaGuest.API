using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.PropertyImages.Commands.DeleteImage;

public record DeleteImageCommand : IRequest<Result>
{
    public Guid ImageId { get; init; }
}
