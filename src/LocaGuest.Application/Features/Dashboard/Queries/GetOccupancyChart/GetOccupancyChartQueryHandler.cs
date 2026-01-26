using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;

public class GetOccupancyChartQueryHandler : IRequestHandler<GetOccupancyChartQuery, Result<OccupancyChartDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetOccupancyChartQueryHandler> _logger;

    public GetOccupancyChartQueryHandler(
        ILocaGuestReadDbContext readDb,
        ICurrentUserService currentUserService,
        ILogger<GetOccupancyChartQueryHandler> logger)
    {
        _readDb = readDb;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<OccupancyChartDto>> Handle(GetOccupancyChartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated)
                return Result.Failure<OccupancyChartDto>("User not authenticated");

            var startDateUtc = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDateUtcExclusive = startDateUtc.AddMonths(1);

            var totalUnits = await _readDb.Properties
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var dailyData = new List<DailyOccupancy>();

            // Fetch only contracts that overlap the selected month.
            var contracts = await _readDb.Contracts
                .AsNoTracking()
                .Where(c => (c.Status == ContractStatus.Active || c.Status == ContractStatus.Signed)
                            && c.StartDate < endDateUtcExclusive
                            && c.EndDate >= startDateUtc)
                .Select(c => new
                {
                    c.PropertyId,
                    c.StartDate,
                    c.EndDate
                })
                .ToListAsync(cancellationToken);

            var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(request.Year, request.Month, day, 0, 0, 0, DateTimeKind.Utc);

                var occupiedUnits = totalUnits == 0
                    ? 0
                    : contracts
                        .Where(c => c.StartDate <= date && c.EndDate >= date)
                        .Select(c => c.PropertyId)
                        .Distinct()
                        .Count();

                var occupancyRate = totalUnits > 0
                    ? (decimal)occupiedUnits / totalUnits * 100m
                    : 0m;

                dailyData.Add(new DailyOccupancy
                {
                    Day = day,
                    Label = $"J{day}",
                    OccupiedUnits = occupiedUnits,
                    TotalUnits = totalUnits,
                    OccupancyRate = Math.Round(occupancyRate, 1)
                });
            }

            _logger.LogInformation("Retrieved occupancy chart for {Month}/{Year}", request.Month, request.Year);

            return Result.Success(new OccupancyChartDto { DailyData = dailyData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving occupancy chart for {Month}/{Year}", request.Month, request.Year);
            return Result.Failure<OccupancyChartDto>("Error retrieving occupancy chart");
        }
    }
}
