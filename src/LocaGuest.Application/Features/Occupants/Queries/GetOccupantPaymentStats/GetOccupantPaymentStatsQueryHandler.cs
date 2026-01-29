using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantPaymentStats;

public class GetOccupantPaymentStatsQueryHandler : IRequestHandler<GetOccupantPaymentStatsQuery, Result<OccupantPaymentStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupantPaymentStatsQueryHandler> _logger;

    public GetOccupantPaymentStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupantPaymentStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OccupantPaymentStatsDto>> Handle(GetOccupantPaymentStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var occupant = await _unitOfWork.Occupants.GetByIdAsync(request.OccupantId, cancellationToken, asNoTracking: true);
            if (occupant == null)
                return Result.Failure<OccupantPaymentStatsDto>($"Occupant with ID {request.OccupantId} not found");

            var allPayments = await _unitOfWork.Payments.Query(asNoTracking: true)
                .Where(p => p.RenterOccupantId == request.OccupantId)
                .ToListAsync(cancellationToken);

            var totalPaid = allPayments.Sum(p => p.AmountPaid);
            var totalPayments = allPayments.Count;
            var latePayments = allPayments.Count(p => p.Status == PaymentStatus.Late || p.Status == PaymentStatus.PaidLate);
            var onTimeRate = totalPayments > 0 ? (decimal)(totalPayments - latePayments) / totalPayments : 1.0m;

            var stats = new OccupantPaymentStatsDto
            {
                OccupantId = request.OccupantId,
                TotalPaid = totalPaid,
                TotalPayments = totalPayments,
                LatePayments = latePayments,
                OnTimeRate = onTimeRate
            };

            _logger.LogInformation("Retrieved payment stats for occupant {OccupantId}: Total={Total}, Payments={Count}, Late={Late}",
                request.OccupantId, totalPaid, totalPayments, latePayments);

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment stats for occupant {OccupantId}", request.OccupantId);
            return Result.Failure<OccupantPaymentStatsDto>("Error retrieving payment stats");
        }
    }
}
