using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsByTenant;

public class GetPaymentsByTenantQueryHandler : IRequestHandler<GetPaymentsByTenantQuery, Result<List<PaymentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPaymentsByTenantQueryHandler> _logger;

    public GetPaymentsByTenantQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPaymentsByTenantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<PaymentDto>>> Handle(GetPaymentsByTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.TenantId, out var tenantId))
            {
                return Result.Failure<List<PaymentDto>>("Invalid tenant ID format");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure<List<PaymentDto>>("Tenant not found");
            }

            var payments = await _unitOfWork.Payments.GetByTenantIdAsync(tenantId, cancellationToken);
            var paymentDtos = new List<PaymentDto>();

            foreach (var payment in payments)
            {
                // Récupérer le contrat pour avoir PaymentDueDay
                var contract = await _unitOfWork.Contracts.GetByIdAsync(payment.ContractId, cancellationToken);
                var paymentDueDay = contract?.PaymentDueDay ?? 5;

                // Calculer la date limite réelle de paiement
                var dueDate = new DateTime(
                    payment.ExpectedDate.Year,
                    payment.ExpectedDate.Month,
                    Math.Min(paymentDueDay, DateTime.DaysInMonth(payment.ExpectedDate.Year, payment.ExpectedDate.Month)),
                    0, 0, 0, DateTimeKind.Utc);

                // Calculer les jours de retard
                int? daysLate = null;
                if (payment.Status != Domain.Aggregates.PaymentAggregate.PaymentStatus.Paid)
                {
                    var today = DateTime.UtcNow.Date;
                    daysLate = (int)(today - dueDate).TotalDays;
                }

                paymentDtos.Add(new PaymentDto
                {
                    Id = payment.Id,
                    TenantId = payment.RenterTenantId,
                    PropertyId = payment.PropertyId,
                    ContractId = payment.ContractId,
                    AmountDue = payment.AmountDue,
                    AmountPaid = payment.AmountPaid,
                    RemainingAmount = payment.GetRemainingAmount(),
                    PaymentDate = payment.PaymentDate,
                    ExpectedDate = payment.ExpectedDate,
                    Status = payment.Status.ToString(),
                    PaymentMethod = payment.PaymentMethod.ToString(),
                    Note = payment.Note,
                    Month = payment.Month,
                    Year = payment.Year,
                    ReceiptId = payment.ReceiptId,
                    CreatedAt = payment.CreatedAt,
                    UpdatedAt = payment.LastModifiedAt,
                    TenantName = tenant.FullName,
                    PaymentDueDay = paymentDueDay,
                    DueDate = dueDate,
                    DaysLate = daysLate
                });
            }

            return Result.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for tenant {TenantId}", request.TenantId);
            return Result.Failure<List<PaymentDto>>("Error retrieving payments");
        }
    }
}
