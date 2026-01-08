using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetContractsByTenant;

public class GetContractsByTenantQueryHandler : IRequestHandler<GetContractsByTenantQuery, Result<List<ContractDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly ILogger<GetContractsByTenantQueryHandler> _logger;

    public GetContractsByTenantQueryHandler(
        IUnitOfWork unitOfWork,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        ILogger<GetContractsByTenantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _logger = logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetContractsByTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.TenantId, out var tenantId))
            {
                return Result.Failure<List<ContractDto>>("Invalid tenant ID format");
            }

            var tenant = await _unitOfWork.Occupants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return Result.Failure<List<ContractDto>>("Tenant not found");
            }

            var contracts = await _unitOfWork.Contracts.GetContractsByTenantAsync(tenantId, cancellationToken);
            
            var contractDtos = new List<ContractDto>();
            
            foreach (var contract in contracts)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);

                var effectiveResult = await _effectiveContractStateResolver.ResolveAsync(
                    contract.Id,
                    DateTime.UtcNow,
                    cancellationToken);

                var effective = effectiveResult.IsSuccess ? effectiveResult.Data : null;
                
                var dto = new ContractDto
                {
                    Id = contract.Id,
                    Code = contract.Code ?? string.Empty,
                    PropertyId = contract.PropertyId,
                    PropertyName = property?.Name ?? "Bien inconnu",
                    TenantId = contract.RenterTenantId,
                    TenantName = tenant.FullName,
                    StartDate = contract.StartDate,
                    EndDate = effective?.EndDate ?? contract.EndDate,
                    Rent = effective?.Rent ?? contract.Rent,
                    Charges = effective?.Charges ?? contract.Charges,
                    Deposit = contract.Deposit,
                    Type = contract.Type.ToString(),
                    Status = contract.Status.ToString(),
                    CreatedAt = contract.CreatedAt,
                    NoticeEndDate = contract.NoticeEndDate,
                    RoomId = effective?.RoomId ?? contract.RoomId,
                    HasInventoryEntry = false,
                    HasInventoryExit = false,
                    PaymentsCount = 0
                };
                
                contractDtos.Add(dto);
            }

            return Result.Success(contractDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for tenant {TenantId}", request.TenantId);
            return Result.Failure<List<ContractDto>>("Error retrieving contracts");
        }
    }
}
