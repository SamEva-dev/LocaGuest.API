using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantContracts;

public record GetOccupantContractsQuery(Guid OccupantId) : IRequest<Result<List<ContractListDto>>>;
