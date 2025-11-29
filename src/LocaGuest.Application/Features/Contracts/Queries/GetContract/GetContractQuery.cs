using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContract;

public record GetContractQuery(Guid ContractId) : IRequest<Result<ContractDto>>;
