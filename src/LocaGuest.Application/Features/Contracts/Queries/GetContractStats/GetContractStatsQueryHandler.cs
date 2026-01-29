using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractStats;

public class GetContractStatsQueryHandler : IRequestHandler<GetContractStatsQuery, Result<ContractStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetContractStatsQueryHandler> _logger;

    public GetContractStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetContractStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ContractStatsDto>> Handle(GetContractStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var threeMonthsFromNow = now.AddMonths(3);

            var activeContracts = await _unitOfWork.Contracts.Query(asNoTracking: true)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync(cancellationToken);

            var expiringContracts = activeContracts
                .Count(c => c.EndDate <= threeMonthsFromNow && c.EndDate > now);

            var monthlyRevenue = activeContracts.Sum(c => c.Rent);

            var totalTenants = await _unitOfWork.Contracts.Query(asNoTracking: true)
                .Where(c => c.Status == ContractStatus.Active)
                .Select(c => c.RenterOccupantId)
                .Distinct()
                .CountAsync(cancellationToken);

            var stats = new ContractStatsDto
            {
                ActiveContracts = activeContracts.Count,
                ExpiringIn3Months = expiringContracts,
                MonthlyRevenue = monthlyRevenue,
                TotalTenants = totalTenants
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract stats");
            return Result.Failure<ContractStatsDto>("Error retrieving contract stats");
        }
    }
}
