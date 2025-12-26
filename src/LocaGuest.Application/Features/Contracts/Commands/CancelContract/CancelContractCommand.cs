using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.CancelContract;

public record CancelContractCommand(Guid ContractId) : IRequest<Result>;
