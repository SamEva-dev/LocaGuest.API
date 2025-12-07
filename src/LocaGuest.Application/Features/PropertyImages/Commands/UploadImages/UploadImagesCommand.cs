using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.PropertyImages.Commands.UploadImages;

public record UploadImagesCommand : IRequest<Result<UploadImagesResponse>>
{
    public Guid PropertyId { get; init; }
    public List<UploadedFileInfo> Files { get; init; } = new();
    public string Category { get; init; } = "other";
}
