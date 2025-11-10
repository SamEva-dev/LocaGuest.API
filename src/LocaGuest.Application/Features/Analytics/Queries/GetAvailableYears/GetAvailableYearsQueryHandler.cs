using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Analytics.Queries.GetAvailableYears;

public class GetAvailableYearsQueryHandler : IRequestHandler<GetAvailableYearsQuery, Result<List<int>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetAvailableYearsQueryHandler> _logger;

    public GetAvailableYearsQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetAvailableYearsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<int>>> Handle(GetAvailableYearsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var contracts = await _context.Contracts.ToListAsync(cancellationToken);
            
            if (!contracts.Any())
            {
                // Si aucun contrat, retourner l'année en cours
                return Result.Success(new List<int> { DateTime.UtcNow.Year });
            }

            var years = contracts
                .SelectMany(c => new[] { c.StartDate.Year, c.EndDate.Year })
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Ajouter l'année en cours si elle n'est pas dans la liste
            var currentYear = DateTime.UtcNow.Year;
            if (!years.Contains(currentYear))
            {
                years.Insert(0, currentYear);
            }

            return Result.Success(years);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available years");
            return Result.Failure<List<int>>("Error retrieving available years");
        }
    }
}
