using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContract;

public class GetContractQueryHandler : IRequestHandler<GetContractQuery, Result<ContractDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetContractQueryHandler> _logger;

    public GetContractQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetContractQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> Handle(GetContractQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure<ContractDto>("Contract not found");

            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(contract.RenterTenantId, cancellationToken);

            var dto = new ContractDto
            {
                Id = contract.Id,
                Code = contract.Code,
                PropertyId = contract.PropertyId,
                PropertyName = property?.Name ?? "Unknown",
                TenantId = contract.RenterTenantId,
                TenantName = tenant?.FullName ?? "Unknown",
                Type = contract.Type.ToString(),
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Rent = contract.Rent,
                Deposit = contract.Deposit,
                Status = contract.Status.ToString()
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract {ContractId}", request.ContractId);
            return Result.Failure<ContractDto>($"Error getting contract: {ex.Message}");
        }
    }
}
