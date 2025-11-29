using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.MarkContractAsExpired;

public record MarkContractAsExpiredCommand(Guid ContractId) : IRequest<Result>;
