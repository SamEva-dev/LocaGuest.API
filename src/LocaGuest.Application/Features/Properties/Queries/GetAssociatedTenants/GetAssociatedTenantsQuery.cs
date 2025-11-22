using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetAssociatedTenants;

public record GetAssociatedTenantsQuery : IRequest<Result<List<TenantDto>>>
{
    public required string PropertyId { get; init; }
}
