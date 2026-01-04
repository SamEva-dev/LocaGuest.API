using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;

public class GetAllContractsQueryHandler : IRequestHandler<GetAllContractsQuery, Result<List<ContractDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllContractsQueryHandler> _logger;

    public GetAllContractsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllContractsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetAllContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Contracts.Query();

            // Filtrer par statut
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ContractStatus>(request.Status, out var status))
            {
                query = query.Where(c => c.Status == status);
            }

            // Filtrer par type
            if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<ContractType>(request.Type, out var type))
            {
                query = query.Where(c => c.Type == type);
            }

            var contracts = await query
                .OrderByDescending(c => c.StartDate)
                .ToListAsync(cancellationToken);

            // Charger les propriétés et locataires
            var propertyIds = contracts.Select(c => c.PropertyId).Distinct().ToList();
            var tenantIds = contracts.Select(c => c.RenterTenantId).Distinct().ToList();

            var properties = await _unitOfWork.Properties.Query()
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var tenants = await _unitOfWork.Occupants.Query()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.FullName, cancellationToken);

            var contractIds = contracts.Select(c => c.Id).ToList();

            var inventoryEntries = await _unitOfWork.InventoryEntries.Query()
                .AsNoTracking()
                .Where(e => contractIds.Contains(e.ContractId))
                .ToDictionaryAsync(e => e.ContractId, e => e.Id, cancellationToken);

            var inventoryExits = await _unitOfWork.InventoryExits.Query()
                .AsNoTracking()
                .Where(e => contractIds.Contains(e.ContractId))
                .ToDictionaryAsync(e => e.ContractId, e => e.Id, cancellationToken);

            var result = contracts.Select(c => new ContractDto
            {
                Id = c.Id,
                Code = c.Code,
                PropertyId = c.PropertyId,
                TenantId = c.RenterTenantId,
                RoomId = c.RoomId,
                IsConflict = c.IsConflict,
                PropertyName = properties.GetValueOrDefault(c.PropertyId),
                TenantName = tenants.GetValueOrDefault(c.RenterTenantId),
                Type = c.Type.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.Rent,
                Charges = c.Charges,
                Deposit = c.Deposit,
                Status = c.Status.ToString(),
                Notes = c.Notes,
                PaymentsCount = c.Payments.Count,
                CreatedAt = c.CreatedAt,
                HasInventoryEntry = inventoryEntries.ContainsKey(c.Id),
                HasInventoryExit = inventoryExits.ContainsKey(c.Id),
                InventoryEntryId = inventoryEntries.GetValueOrDefault(c.Id),
                InventoryExitId = inventoryExits.GetValueOrDefault(c.Id),
                NoticeEndDate = c.NoticeEndDate
            }).ToList();

            // Filtrer par terme de recherche après chargement des noms
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                result = result.Where(c =>
                    (c.PropertyName?.ToLower().Contains(searchLower) ?? false) ||
                    (c.TenantName?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts");
            return Result.Failure<List<ContractDto>>("Error retrieving contracts");
        }
    }
}
