using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Tenants;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenants;

public record GetTenantsQuery : IRequest<Result<PagedResult<TenantDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public string? Status { get; init; }
}
