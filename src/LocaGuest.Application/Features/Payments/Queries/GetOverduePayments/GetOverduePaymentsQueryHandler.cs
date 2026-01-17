using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Payments;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Queries.GetOverduePayments;

public class GetOverduePaymentsQueryHandler : IRequestHandler<GetOverduePaymentsQuery, Result<List<PaymentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetOverduePaymentsQueryHandler> _logger;

    public GetOverduePaymentsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetOverduePaymentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<PaymentDto>>> Handle(GetOverduePaymentsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Result<List<PaymentDto>>.Failure<List<PaymentDto>>("User not authenticated");
        }

        try
        {
            // Get all payments
            var payments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);

            // Filter for overdue (Late status)
            var overduePayments = payments.Where(p => 
                p.Status == PaymentStatus.Late || 
                (p.Status == PaymentStatus.Partial && p.ExpectedDate < DateTime.UtcNow));

            // Apply filters
            if (request.PropertyId.HasValue)
            {
                overduePayments = overduePayments.Where(p => p.PropertyId == request.PropertyId.Value);
            }

            if (request.OccupantId.HasValue)
            {
                overduePayments = overduePayments.Where(p => p.RenterOccupantId == request.OccupantId.Value);
            }

            if (request.MaxDaysLate.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-request.MaxDaysLate.Value);
                overduePayments = overduePayments.Where(p => p.ExpectedDate >= cutoffDate);
            }

            // Order by expected date (oldest first)
            var orderedPayments = overduePayments
                .OrderBy(p => p.ExpectedDate)
                .ToList();

            // Map to DTOs
            var paymentDtos = orderedPayments.Select(p => new PaymentDto
            {
                Id = p.Id,
                OccupantId = p.RenterOccupantId,
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
                InvoiceDocumentId = p.InvoiceDocumentId,
                DaysLate = (int)(DateTime.UtcNow.Date - p.ExpectedDate.Date).TotalDays
            }).ToList();

            _logger.LogInformation("Retrieved {Count} overdue payments", paymentDtos.Count);

            return Result<List<PaymentDto>>.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue payments");
            return Result<List<PaymentDto>>.Failure<List<PaymentDto>>("Failed to retrieve overdue payments");
        }
    }
}
