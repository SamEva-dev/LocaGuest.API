using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Deposits.Commands.RecordDepositReceived;

public record RecordDepositReceivedCommand(
    Guid ContractId,
    decimal Amount,
    DateTime DateUtc,
    string? Reference) : IRequest<Result<Guid>>;
