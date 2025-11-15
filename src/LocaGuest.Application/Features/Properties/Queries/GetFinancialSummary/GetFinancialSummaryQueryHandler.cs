using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetFinancialSummary;

public class GetFinancialSummaryQueryHandler : IRequestHandler<GetFinancialSummaryQuery, Result<FinancialSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetFinancialSummaryQueryHandler> _logger;

    public GetFinancialSummaryQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetFinancialSummaryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<FinancialSummaryDto>> Handle(GetFinancialSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            var contracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.PropertyId == propertyId)
                .Include(c => c.Payments)
                .ToListAsync(cancellationToken);

            var activeContracts = contracts.Count(c => c.Status == ContractStatus.Active);
            var allPayments = contracts.SelectMany(c => c.Payments).ToList();
            var totalRevenue = allPayments.Sum(p => p.Amount);
            var monthlyRent = contracts.Where(c => c.Status == ContractStatus.Active).Sum(c => c.Rent);
            
            var lastPayment = allPayments
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            // Calculer le taux d'occupation (simple: pourcentage du temps avec contrat actif)
            var occupancyRate = activeContracts > 0 ? 100m : 0m;

            var summary = new FinancialSummaryDto
            {
                PropertyId = propertyId,
                TotalRevenue = totalRevenue,
                MonthlyRent = monthlyRent,
                LastPaymentAmount = lastPayment?.Amount,
                LastPaymentDate = lastPayment?.PaymentDate,
                OccupancyRate = occupancyRate,
                TotalPayments = allPayments.Count,
                ActiveContracts = activeContracts
            };

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial summary for property {PropertyId}", request.PropertyId);
            return Result.Failure<FinancialSummaryDto>("Error retrieving financial summary");
        }
    }
}
