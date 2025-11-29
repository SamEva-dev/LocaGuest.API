using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.ActivateContract;

public record ActivateContractCommand(Guid ContractId) : IRequest<Result>;
