using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
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

            var payments = await _unitOfWork.Contracts.Query()
                .Where(c => c.PropertyId == propertyId)
                .SelectMany(c => c.Payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    ContractId = p.ContractId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Method = p.Method.ToString(),
                    Status = p.Status.ToString()
                }))
                .OrderByDescending(p => p.PaymentDate)
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
