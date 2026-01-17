using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Commands.ChangeOccupantStatus;

public record ChangeOccupantStatusCommand : IRequest<Result<bool>>
{
    public required Guid OccupantId { get; init; }
    public required OccupantStatus Status { get; init; }
}
