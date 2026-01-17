using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractsByTenant;

public record GetContractsByTenantQuery : IRequest<Result<List<ContractDto>>>
{
    public required string OccupantId { get; init; }
}
