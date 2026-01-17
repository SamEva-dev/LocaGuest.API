using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetPropertyPayments;

public class GetPropertyPaymentsQueryHandler : IRequestHandler<GetPropertyPaymentsQuery, Result<List<PaymentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPropertyPaymentsQueryHandler> _logger;

    public GetPropertyPaymentsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPropertyPaymentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<PaymentDto>>> Handle(GetPropertyPaymentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            var payments = await _unitOfWork.Payments.Query()
                .Where(p => p.PropertyId == propertyId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    OccupantId = p.RenterOccupantId,
                    PropertyId = p.PropertyId,
                    ContractId = p.ContractId,
                    PaymentType = p.PaymentType.ToString(),
                    AmountDue = p.AmountDue,
                    AmountPaid = p.AmountPaid,
                    RemainingAmount = p.AmountDue - p.AmountPaid,
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
                    UpdatedAt = p.LastModifiedAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<PaymentDto>>("Error retrieving property payments");
        }
    }
}
