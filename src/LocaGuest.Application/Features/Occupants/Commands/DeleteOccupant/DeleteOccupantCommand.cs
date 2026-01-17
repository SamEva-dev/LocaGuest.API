using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Commands.DeleteOccupant;

public record DeleteOccupantCommand : IRequest<Result<DeleteOccupantResult>>
{
    public Guid OccupantId { get; init; }
}

public record DeleteOccupantResult
{
    public Guid Id { get; init; }
    public int DeletedContracts { get; init; }
    public int DeletedPayments { get; init; }
    public int DeletedDocuments { get; init; }
}
