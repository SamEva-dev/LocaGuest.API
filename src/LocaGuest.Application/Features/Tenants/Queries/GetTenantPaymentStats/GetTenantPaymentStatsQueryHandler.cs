using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenantPaymentStats;

public class GetTenantPaymentStatsQueryHandler : IRequestHandler<GetTenantPaymentStatsQuery, Result<TenantPaymentStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantPaymentStatsQueryHandler> _logger;

    public GetTenantPaymentStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantPaymentStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantPaymentStatsDto>> Handle(GetTenantPaymentStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure<TenantPaymentStatsDto>($"Tenant with ID {request.TenantId} not found");

            var allPayments = await _unitOfWork.Payments.Query()
                .Where(p => p.RenterTenantId == request.TenantId)
                .ToListAsync(cancellationToken);

            var totalPaid = allPayments.Sum(p => p.AmountPaid);
            var totalPayments = allPayments.Count;
            var latePayments = allPayments.Count(p => p.Status == PaymentStatus.Late || p.Status == PaymentStatus.PaidLate);
            var onTimeRate = totalPayments > 0 ? (decimal)(totalPayments - latePayments) / totalPayments : 1.0m;

            var stats = new TenantPaymentStatsDto
            {
                TenantId = request.TenantId,
                TotalPaid = totalPaid,
                TotalPayments = totalPayments,
                LatePayments = latePayments,
                OnTimeRate = onTimeRate
            };

            _logger.LogInformation("Retrieved payment stats for tenant {TenantId}: Total={Total}, Payments={Count}, Late={Late}",
                request.TenantId, totalPaid, totalPayments, latePayments);

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment stats for tenant {TenantId}", request.TenantId);
            return Result.Failure<TenantPaymentStatsDto>("Error retrieving payment stats");
        }
    }
}
