using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateContract;

public record CreateContractCommand : IRequest<Result<ContractDto>>
{
    public Guid PropertyId { get; init; }
    public Guid TenantId { get; init; }
    public string Type { get; init; } = "Unfurnished";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal Rent { get; init; }
    public decimal? Deposit { get; init; }
    public string? Notes { get; init; }
}
