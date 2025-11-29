using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContracts;

public class GetContractsQueryHandler : IRequestHandler<GetContractsQuery, Result<ContractsPagedResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetContractsQueryHandler> _logger;

    public GetContractsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetContractsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ContractsPagedResult>> Handle(GetContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var spec = new ContractsSpec(request.Status, request.Page, request.PageSize);
            var contracts = await _unitOfWork.Contracts.FindAsync(spec.Criteria, cancellationToken);
            var total = await _unitOfWork.Contracts.CountAsync(spec.Criteria, cancellationToken);

            var dtos = new List<ContractListDto>();
            foreach (var contract in contracts.OrderByDescending(c => c.StartDate)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize))
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(contract.RenterTenantId, cancellationToken);

                dtos.Add(new ContractListDto
                {
                    Id = contract.Id,
                    PropertyId = contract.PropertyId,
                    PropertyName = property?.Name ?? "Unknown",
                    TenantId = contract.RenterTenantId,
                    TenantName = tenant?.FullName ?? "Unknown",
                    Type = contract.Type.ToString(),
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    Rent = contract.Rent,
                    Deposit = contract.Deposit ?? 0,
                    Status = contract.Status.ToString()
                });
            }

            var result = new ContractsPagedResult(total, request.Page, request.PageSize, dtos);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contracts");
            return Result.Failure<ContractsPagedResult>($"Error getting contracts: {ex.Message}");
        }
    }
}

public class ContractsSpec
{
    public System.Linq.Expressions.Expression<Func<Contract, bool>> Criteria { get; }

    public ContractsSpec(string? status, int page, int pageSize)
    {
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContractStatus>(status, true, out var statusEnum))
        {
            Criteria = c => c.Status == statusEnum;
        }
        else
        {
            Criteria = c => true;
        }
    }
}
