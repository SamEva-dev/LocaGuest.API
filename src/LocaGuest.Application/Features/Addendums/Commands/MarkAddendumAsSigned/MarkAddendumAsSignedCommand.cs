using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Addendums.Commands.MarkAddendumAsSigned;

public record MarkAddendumAsSignedCommand : IRequest<Result<Guid>>
{
    public required Guid AddendumId { get; init; }
    public DateTime? SignedDateUtc { get; init; }
    public string? SignedBy { get; init; }
}
