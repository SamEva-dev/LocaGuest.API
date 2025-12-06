using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentStats;

public class GetPaymentStatsQueryHandler : IRequestHandler<GetPaymentStatsQuery, Result<PaymentStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPaymentStatsQueryHandler> _logger;

    public GetPaymentStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPaymentStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaymentStatsDto>> Handle(GetPaymentStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            List<Payment> payments;

            // Récupérer les paiements selon les filtres
            if (!string.IsNullOrEmpty(request.TenantId))
            {
                if (!Guid.TryParse(request.TenantId, out var tenantId))
                {
                    return Result.Failure<PaymentStatsDto>("Invalid tenant ID format");
                }
                payments = await _unitOfWork.Payments.GetByTenantIdAsync(tenantId, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.PropertyId))
            {
                if (!Guid.TryParse(request.PropertyId, out var propertyId))
                {
                    return Result.Failure<PaymentStatsDto>("Invalid property ID format");
                }
                payments = await _unitOfWork.Payments.GetByPropertyIdAsync(propertyId, cancellationToken);
            }
            else
            {
                return Result.Failure<PaymentStatsDto>("Either TenantId or PropertyId must be provided");
            }

            // Filtrer par mois/année si fourni
            if (request.Month.HasValue && request.Year.HasValue)
            {
                payments = payments
                    .Where(p => p.Month == request.Month.Value && p.Year == request.Year.Value)
                    .ToList();
            }
            else if (request.Year.HasValue)
            {
                payments = payments
                    .Where(p => p.Year == request.Year.Value)
                    .ToList();
            }

            // Calculer les statistiques
            var stats = new PaymentStatsDto
            {
                TotalExpected = payments.Sum(p => p.AmountDue),
                TotalPaid = payments.Sum(p => p.AmountPaid),
                TotalRemaining = payments.Sum(p => p.GetRemainingAmount()),
                CountPaid = payments.Count(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PaidLate),
                CountLate = payments.Count(p => p.Status == PaymentStatus.Late || p.Status == PaymentStatus.PaidLate),
                CountPending = payments.Count(p => p.Status == PaymentStatus.Pending),
                CountPartial = payments.Count(p => p.Status == PaymentStatus.Partial)
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payment stats");
            return Result.Failure<PaymentStatsDto>("Error calculating payment statistics");
        }
    }
}
