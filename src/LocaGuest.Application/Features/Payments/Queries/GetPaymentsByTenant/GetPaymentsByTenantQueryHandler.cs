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

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                PropertyId = p.PropertyId,
                ContractId = p.ContractId,
                AmountDue = p.AmountDue,
                AmountPaid = p.AmountPaid,
                RemainingAmount = p.GetRemainingAmount(),
                PaymentDate = p.PaymentDate,
                ExpectedDate = p.ExpectedDate,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                Note = p.Note,
                Month = p.Month,
                Year = p.Year,
                ReceiptId = p.ReceiptId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.LastModifiedAt,
                TenantName = tenant.FullName
            }).ToList();

            return Result.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for tenant {TenantId}", request.TenantId);
            return Result.Failure<List<PaymentDto>>("Error retrieving payments");
        }
    }
}
