using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Analytics;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Queries.GetProfitabilityStats;

public class GetProfitabilityStatsQueryHandler : IRequestHandler<GetProfitabilityStatsQuery, Result<ProfitabilityStatsDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetProfitabilityStatsQueryHandler> _logger;

    public GetProfitabilityStatsQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetProfitabilityStatsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ProfitabilityStatsDto>> Handle(GetProfitabilityStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            // Revenus du mois en cours (loyers des contrats actifs)
            var activeContracts = await _context.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync(cancellationToken);

            var monthlyRevenue = activeContracts.Sum(c => c.Rent);

            // Revenus du mois précédent
            var lastMonthContracts = await _context.Contracts
                .Where(c => c.StartDate <= lastMonthEnd && c.EndDate >= lastMonthStart)
                .ToListAsync(cancellationToken);

            var lastMonthRevenue = lastMonthContracts.Sum(c => c.Rent);

            // Charges du mois (simulation - à adapter selon votre modèle)
            var monthlyExpenses = monthlyRevenue * 0.15m; // 15% de charges estimées
            var lastMonthExpenses = lastMonthRevenue * 0.15m;

            // Calculs
            var netProfit = monthlyRevenue - monthlyExpenses;
            var lastMonthProfit = lastMonthRevenue - lastMonthExpenses;

            var revenueChange = lastMonthRevenue > 0 
                ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 
                : 0;

            var expensesChange = lastMonthExpenses > 0 
                ? ((monthlyExpenses - lastMonthExpenses) / lastMonthExpenses) * 100 
                : 0;

            var profitChange = lastMonthProfit > 0 
                ? ((netProfit - lastMonthProfit) / lastMonthProfit) * 100 
                : 0;

            // Taux de rentabilité (ROI annuel estimé)
            // Estimation: valeur du bien = loyer annuel x 20
            var properties = await _context.Properties.ToListAsync(cancellationToken);
            var totalPropertyValue = properties.Sum(p => p.Rent * 12 * 20);

            var profitabilityRate = totalPropertyValue > 0 
                ? (netProfit * 12 / totalPropertyValue) * 100 
                : 0;

            var stats = new ProfitabilityStatsDto
            {
                MonthlyRevenue = monthlyRevenue,
                MonthlyExpenses = monthlyExpenses,
                NetProfit = netProfit,
                ProfitabilityRate = Math.Round(profitabilityRate, 1),
                RevenueChangePercent = Math.Round(revenueChange, 1),
                ExpensesChangePercent = Math.Round(expensesChange, 1),
                ProfitChangePercent = Math.Round(profitChange, 1),
                TargetRate = 8.5m
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profitability stats");
            return Result.Failure<ProfitabilityStatsDto>("Error retrieving profitability stats");
        }
    }
}
