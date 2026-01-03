using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetOccupancyChart;

public class GetOccupancyChartQueryHandler : IRequestHandler<GetOccupancyChartQuery, Result<OccupancyChartDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetOccupancyChartQueryHandler> _logger;

    public GetOccupancyChartQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetOccupancyChartQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<OccupancyChartDto>> Handle(GetOccupancyChartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated)
                return Result.Failure<OccupancyChartDto>("User not authenticated");

            var monthlyData = new List<MonthlyOccupancy>();
            var properties = await _unitOfWork.Properties.GetAllAsync(cancellationToken);
            var totalUnits = properties.Count();

            if (totalUnits == 0)
            {
                // Retourner des données vides si aucune propriété
                for (int month = 1; month <= 12; month++)
                {
                    monthlyData.Add(new MonthlyOccupancy
                    {
                        Month = month,
                        MonthName = new DateTime(request.Year, month, 1).ToString("MMMM", CultureInfo.GetCultureInfo("fr-FR")),
                        OccupiedUnits = 0,
                        TotalUnits = 0,
                        OccupancyRate = 0
                    });
                }
            }
            else
            {
                var contracts = await _unitOfWork.Contracts.GetAllAsync(cancellationToken);

                for (int month = 1; month <= 12; month++)
                {
                    var firstDayOfMonth = new DateTime(request.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    // Compter les contrats actifs pendant ce mois
                    var activeContractsInMonth = contracts
                        .Where(c =>
                            (c.Status == ContractStatus.Active || c.Status == ContractStatus.Signed) &&
                            c.StartDate <= lastDayOfMonth &&
                            c.EndDate >= firstDayOfMonth)
                        .Count();

                    var occupancyRate = totalUnits > 0 ? (decimal)activeContractsInMonth / totalUnits * 100 : 0;

                    monthlyData.Add(new MonthlyOccupancy
                    {
                        Month = month,
                        MonthName = firstDayOfMonth.ToString("MMMM", CultureInfo.GetCultureInfo("fr-FR")),
                        OccupiedUnits = activeContractsInMonth,
                        TotalUnits = totalUnits,
                        OccupancyRate = Math.Round(occupancyRate, 1)
                    });
                }
            }

            _logger.LogInformation("Retrieved occupancy chart for year {Year}", request.Year);

            return Result.Success(new OccupancyChartDto
            {
                MonthlyData = monthlyData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving occupancy chart for year {Year}", request.Year);
            return Result.Failure<OccupancyChartDto>($"Error retrieving occupancy chart: {ex.Message}");
        }
    }
}
