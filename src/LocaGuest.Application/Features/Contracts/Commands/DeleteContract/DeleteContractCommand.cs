using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.DeleteContract;

public record DeleteContractCommand(Guid ContractId) : IRequest<Result<DeleteContractResultDto>>;
