using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenant;

public record GetTenantQuery : IRequest<Result<TenantDto>>
{
    public required string Id { get; init; }
}
