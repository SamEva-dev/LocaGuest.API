using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;

public record GetAllContractsQuery : IRequest<Result<List<ContractDto>>>
{
    public string? SearchTerm { get; init; }
    public string? Status { get; init; }
    public string? Type { get; init; }
}
