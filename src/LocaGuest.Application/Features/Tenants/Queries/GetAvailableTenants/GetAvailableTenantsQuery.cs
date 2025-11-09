using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Queries.GetAvailableTenants;

public record GetAvailableTenantsQuery : IRequest<Result<List<TenantDto>>>
{
    public required string PropertyId { get; init; }
}
