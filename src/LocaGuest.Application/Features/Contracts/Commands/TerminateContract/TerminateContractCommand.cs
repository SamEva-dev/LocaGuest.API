using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.TerminateContract;

public record TerminateContractCommand : IRequest<Result>
{
    public Guid ContractId { get; init; }
    public DateTime TerminationDate { get; init; }
    public string Reason { get; init; } = string.Empty;
}
