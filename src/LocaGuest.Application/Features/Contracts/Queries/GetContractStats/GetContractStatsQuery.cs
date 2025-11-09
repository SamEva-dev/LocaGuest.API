using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractStats;

public record GetContractStatsQuery : IRequest<Result<ContractStatsDto>>
{
}

public class ContractStatsDto
{
    public int ActiveContracts { get; set; }
    public int ExpiringIn3Months { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalTenants { get; set; }
}
