using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.CancelNotice;

public record CancelNoticeCommand : IRequest<Result>
{
    public Guid ContractId { get; init; }
}
