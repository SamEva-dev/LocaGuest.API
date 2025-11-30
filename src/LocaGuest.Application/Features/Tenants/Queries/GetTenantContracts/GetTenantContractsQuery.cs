using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenantContracts;

public record GetTenantContractsQuery(Guid TenantId) : IRequest<Result<List<ContractListDto>>>;
