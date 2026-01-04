using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Commands.ChangeOccupantStatus;

public record ChangeOccupantStatusCommand : IRequest<Result<bool>>
{
    public required Guid TenantId { get; init; }
    public required OccupantStatus Status { get; init; }
}
