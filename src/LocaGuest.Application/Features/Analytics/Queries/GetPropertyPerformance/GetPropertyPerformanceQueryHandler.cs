using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Analytics;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Queries.GetPropertyPerformance;

public class GetPropertyPerformanceQueryHandler : IRequestHandler<GetPropertyPerformanceQuery, Result<List<PropertyPerformanceDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetPropertyPerformanceQueryHandler> _logger;

    public GetPropertyPerformanceQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetPropertyPerformanceQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<PropertyPerformanceDto>>> Handle(GetPropertyPerformanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var properties = await _context.Properties.ToListAsync(cancellationToken);
            var result = new List<PropertyPerformanceDto>();

            foreach (var property in properties)
            {
                // Contrats actifs pour cette propriété
                var activeContracts = await _context.Contracts
                    .Where(c => c.PropertyId == property.Id && 
                               c.Status == ContractStatus.Active &&
                               c.EndDate >= DateTime.UtcNow &&
                               c.StartDate <= DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                var revenue = activeContracts.Sum(c => c.Rent);
                var expenses = revenue * 0.20m; // 20% de charges estimées par propriété
                var netProfit = revenue - expenses;

                // ROI basé sur une estimation de valeur (loyer annuel x 20)
                var estimatedValue = property.Rent * 12 * 20;
                var roi = estimatedValue > 0
                    ? (netProfit * 12 / estimatedValue) * 100
                    : 0;

                result.Add(new PropertyPerformanceDto
                {
                    PropertyId = property.Id,
                    PropertyName = property.Name,
                    Revenue = revenue,
                    Expenses = expenses,
                    NetProfit = netProfit,
                    ROI = Math.Round(roi, 1)
                });
            }

            return Result.Success(result.OrderByDescending(p => p.ROI).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property performance");
            return Result.Failure<List<PropertyPerformanceDto>>("Error retrieving property performance");
        }
    }
}
