using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Deposits.Queries.GetDepositReceiptByContract;

public record GetDepositReceiptByContractQuery(Guid ContractId) : IRequest<Result<byte[]>>;
