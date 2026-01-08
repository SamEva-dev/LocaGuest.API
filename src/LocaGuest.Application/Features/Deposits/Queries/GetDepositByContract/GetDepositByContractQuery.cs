using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Deposits;
using MediatR;

namespace LocaGuest.Application.Features.Deposits.Queries.GetDepositByContract;

public record GetDepositByContractQuery(Guid ContractId) : IRequest<Result<DepositDto>>;
