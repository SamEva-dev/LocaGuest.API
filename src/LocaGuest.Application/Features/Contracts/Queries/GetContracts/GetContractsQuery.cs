using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContracts;

public record GetContractsQuery : IRequest<Result<ContractsPagedResult>>
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record ContractsPagedResult(int Total, int Page, int PageSize, List<ContractListDto> Data);
