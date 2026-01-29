using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Analytics;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LocaGuest.Application.Features.Analytics.Queries.GetRevenueEvolution;

public class GetRevenueEvolutionQueryHandler : IRequestHandler<GetRevenueEvolutionQuery, Result<List<RevenueEvolutionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetRevenueEvolutionQueryHandler> _logger;

    public GetRevenueEvolutionQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetRevenueEvolutionQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<RevenueEvolutionDto>>> Handle(GetRevenueEvolutionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = new List<RevenueEvolutionDto>();
            var now = DateTime.UtcNow;
            
            // Si une année est spécifiée, utiliser cette année, sinon utiliser l'année courante
            var targetYear = request.Year ?? now.Year;
            var referenceDate = new DateTime(targetYear, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int i = request.Months - 1; i >= 0; i--)
            {
                var monthDate = referenceDate.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Contrats actifs ce mois-là
                var monthContracts = await _unitOfWork.Contracts.Query(asNoTracking: true)
                    .Where(c => c.StartDate <= monthEnd && c.EndDate >= monthStart)
                    .ToListAsync(cancellationToken);

                var revenue = monthContracts.Sum(c => c.Rent);
                var expenses = revenue * 0.15m; // 15% de charges estimées

                result.Add(new RevenueEvolutionDto
                {
                    Month = monthDate.ToString("MMM", CultureInfo.GetCultureInfo("fr-FR")),
                    Revenue = revenue,
                    Expenses = expenses
                });
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue evolution");
            return Result.Failure<List<RevenueEvolutionDto>>("Error retrieving revenue evolution");
        }
    }
}
