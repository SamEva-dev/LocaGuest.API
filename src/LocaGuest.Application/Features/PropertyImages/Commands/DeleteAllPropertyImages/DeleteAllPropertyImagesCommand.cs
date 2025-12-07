using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.PropertyImages.Commands.DeleteAllPropertyImages;

public record DeleteAllPropertyImagesCommand : IRequest<Result>
{
    public Guid PropertyId { get; init; }
}
