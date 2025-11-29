using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.MarkContractAsSigned;

public record MarkContractAsSignedCommand : IRequest<Result>
{
    public Guid ContractId { get; init; }
    public DateTime? SignedDate { get; init; }
}
