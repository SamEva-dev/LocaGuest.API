using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;

public record GetPropertyContractsQuery : IRequest<Result<List<ContractDto>>>
{
    public required string PropertyId { get; init; }
}
