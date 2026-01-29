using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDeadlines;

public class GetDeadlinesQueryHandler : IRequestHandler<GetDeadlinesQuery, Result<DeadlinesDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetDeadlinesQueryHandler> _logger;

    public GetDeadlinesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetDeadlinesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<DeadlinesDto>> Handle(GetDeadlinesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated)
                return Result.Failure<DeadlinesDto>("User not authenticated");

            var deadlines = new List<DeadlineItem>();
            var today = DateTime.UtcNow.Date;
            var nextMonth = today.AddDays(30);

            // 1. Prochaines échéances de loyer (paiements attendus)
            var activeContracts = await _unitOfWork.Contracts
                .GetAllAsync(cancellationToken, asNoTracking: true);

            var contractsList = activeContracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToList();

            foreach (var contract in contractsList)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken, asNoTracking: true);
                var tenant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken, asNoTracking: true);

                if (property == null || tenant == null)
                    continue;

                // Prochaine date de paiement basée sur PaymentDueDay
                var nextPaymentDate = new DateTime(today.Year, today.Month, Math.Min(contract.PaymentDueDay, DateTime.DaysInMonth(today.Year, today.Month)), 0, 0, 0, DateTimeKind.Utc);
                
                if (nextPaymentDate < today)
                {
                    // Si date dépassée ce mois, prendre le mois prochain
                    var followingMonth = today.AddMonths(1);
                    nextPaymentDate = new DateTime(followingMonth.Year, followingMonth.Month, Math.Min(contract.PaymentDueDay, DateTime.DaysInMonth(followingMonth.Year, followingMonth.Month)), 0, 0, 0, DateTimeKind.Utc);
                }

                if (nextPaymentDate <= nextMonth)
                {
                    deadlines.Add(new DeadlineItem
                    {
                        Type = "Rent",
                        Title = "Loyer à percevoir",
                        Description = $"{contract.Rent + contract.Charges:F2}€",
                        Date = nextPaymentDate,
                        PropertyCode = property.Code,
                        OccupantName = tenant.FullName
                    });
                }
            }

            // 2. Contrats arrivant à expiration (30 jours)
            var expiringContracts = contractsList
                .Where(c => c.EndDate >= today && c.EndDate <= nextMonth)
                .ToList();

            foreach (var contract in expiringContracts)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken, asNoTracking: true);
                var tenant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken, asNoTracking: true);

                if (property == null || tenant == null)
                    continue;

                deadlines.Add(new DeadlineItem
                {
                    Type = "Contract",
                    Title = "Fin de bail",
                    Description = $"Contrat {contract.Code}",
                    Date = contract.EndDate,
                    PropertyCode = property.Code,
                    OccupantName = tenant.FullName
                });
            }

            // Trier par date
            var sortedDeadlines = deadlines
                .OrderBy(d => d.Date)
                .Take(10)
                .ToList();

            _logger.LogInformation("Retrieved {Count} upcoming deadlines", sortedDeadlines.Count);

            return Result.Success(new DeadlinesDto
            {
                UpcomingDeadlines = sortedDeadlines
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deadlines");
            return Result.Failure<DeadlinesDto>($"Error retrieving deadlines: {ex.Message}");
        }
    }
}
