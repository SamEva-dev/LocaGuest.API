using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsByProperty;

public class GetPaymentsByPropertyQueryHandler : IRequestHandler<GetPaymentsByPropertyQuery, Result<List<PaymentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPaymentsByPropertyQueryHandler> _logger;

    public GetPaymentsByPropertyQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPaymentsByPropertyQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<PaymentDto>>> Handle(GetPaymentsByPropertyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.PropertyId, out var propertyId))
            {
                return Result.Failure<List<PaymentDto>>("Invalid property ID format");
            }

            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);
            if (property == null)
            {
                return Result.Failure<List<PaymentDto>>("Property not found");
            }

            var payments = await _unitOfWork.Payments.GetByPropertyIdAsync(propertyId, cancellationToken);

            // Enrichir avec les noms de locataires
            var tenantIds = payments.Select(p => p.RenterTenantId).Distinct();
            var tenants = new Dictionary<Guid, string>();
            
            foreach (var tenantId in tenantIds)
            {
                var tenant = await _unitOfWork.Occupants.GetByIdAsync(tenantId, cancellationToken);
                if (tenant != null)
                {
                    tenants[tenantId] = tenant.FullName;
                }
            }

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                TenantId = p.RenterTenantId,
                PropertyId = p.PropertyId,
                ContractId = p.ContractId,
                PaymentType = p.PaymentType.ToString(),
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
                InvoiceDocumentId = p.InvoiceDocumentId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.LastModifiedAt,
                TenantName = tenants.GetValueOrDefault(p.RenterTenantId),
                PropertyName = property.Name
            }).ToList();

            return Result.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<PaymentDto>>("Error retrieving payments");
        }
    }
}
