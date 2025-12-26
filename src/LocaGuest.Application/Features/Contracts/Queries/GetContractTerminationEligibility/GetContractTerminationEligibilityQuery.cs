using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractTerminationEligibility;

public record GetContractTerminationEligibilityQuery(Guid ContractId) : IRequest<Result<ContractTerminationEligibilityDto>>;
