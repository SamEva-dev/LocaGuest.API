using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Queries.GetOccupancyTrend;

public class GetOccupancyTrendQueryHandler : IRequestHandler<GetOccupancyTrendQuery, Result<List<OccupancyDataPointDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupancyTrendQueryHandler> _logger;

    public GetOccupancyTrendQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupancyTrendQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<OccupancyDataPointDto>>> Handle(GetOccupancyTrendQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = new List<OccupancyDataPointDto>();
            var now = DateTime.UtcNow;
            var totalProperties = await _unitOfWork.Properties.Query().CountAsync(cancellationToken);

            if (totalProperties == 0)
            {
                // Return empty data if no properties
                for (int i = request.Days - 1; i >= 0; i--)
                {
                    result.Add(new OccupancyDataPointDto
                    {
                        Date = now.AddDays(-i),
                        OccupancyRate = 0
                    });
                }
                return Result.Success(result);
            }

            // Pour chaque jour des N derniers jours
            for (int i = request.Days - 1; i >= 0; i--)
            {
                var targetDate = now.AddDays(-i);
                var dayStart = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);

                // Compter les contrats actifs ce jour-là
                var activeContractsCount = await _unitOfWork.Contracts.Query()
                    .Where(c => c.Status == ContractStatus.Active &&
                                c.StartDate <= dayEnd && 
                                c.EndDate >= dayStart)
                    .CountAsync(cancellationToken);

                var occupancyRate = totalProperties > 0 
                    ? Math.Round((decimal)activeContractsCount / totalProperties * 100, 1)
                    : 0;

                result.Add(new OccupancyDataPointDto
                {
                    Date = targetDate,
                    OccupancyRate = occupancyRate
                });
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving occupancy trend");
            return Result.Failure<List<OccupancyDataPointDto>>("Error retrieving occupancy trend");
        }
    }
}
