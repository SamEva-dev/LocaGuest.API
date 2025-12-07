using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.PropertyImages.Queries.GetImage;

public record GetImageQuery : IRequest<Result<ImageFileResult>>
{
    public Guid ImageId { get; init; }
}

public class ImageFileResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
