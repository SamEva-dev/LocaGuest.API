using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.PropertyImages.Queries.GetPropertyImages;

public record GetPropertyImagesQuery : IRequest<Result<List<PropertyImageDto>>>
{
    public Guid PropertyId { get; init; }
}
