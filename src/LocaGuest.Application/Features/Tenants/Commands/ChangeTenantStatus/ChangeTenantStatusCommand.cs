using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Commands.ChangeTenantStatus;

public record ChangeTenantStatusCommand : IRequest<Result<bool>>
{
    public required Guid TenantId { get; init; }
    public required TenantStatus Status { get; init; }
}
