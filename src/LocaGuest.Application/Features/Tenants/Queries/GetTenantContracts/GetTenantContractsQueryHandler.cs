using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenantContracts;

public class GetTenantContractsQueryHandler : IRequestHandler<GetTenantContractsQuery, Result<List<ContractListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantContractsQueryHandler> _logger;

    public GetTenantContractsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantContractsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<ContractListDto>>> Handle(GetTenantContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure<List<ContractListDto>>($"Tenant with ID {request.TenantId} not found");

            var contracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.RenterTenantId == request.TenantId)
                .Include(c => c.Payments)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync(cancellationToken);

            var contractDtos = contracts.Select(c => new ContractListDto
            {
                Id = c.Id,
                PropertyId = c.PropertyId,
                PropertyName = string.Empty, // TODO: Load from property if needed
                TenantId = c.RenterTenantId,
                TenantName = string.Empty, // TODO: Load from tenant if needed
                Type = c.Type.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.Rent,
                Deposit = c.Deposit ?? 0,
                Status = c.Status.ToString()
            }).ToList();

            _logger.LogInformation("Retrieved {Count} contracts for tenant {TenantId}", contractDtos.Count, request.TenantId);
            return Result.Success(contractDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for tenant {TenantId}", request.TenantId);
            return Result.Failure<List<ContractListDto>>("Error retrieving tenant contracts");
        }
    }
}
