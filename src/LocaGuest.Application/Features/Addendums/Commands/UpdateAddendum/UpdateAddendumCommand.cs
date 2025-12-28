using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Addendums;
using MediatR;

namespace LocaGuest.Application.Features.Addendums.Commands.UpdateAddendum;

public record UpdateAddendumCommand : IRequest<Result<AddendumDto>>
{
    public Guid Id { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public string? Reason { get; init; }
    public string? Description { get; init; }

    public List<Guid>? AttachedDocumentIds { get; init; }
    public string? Notes { get; init; }
}
