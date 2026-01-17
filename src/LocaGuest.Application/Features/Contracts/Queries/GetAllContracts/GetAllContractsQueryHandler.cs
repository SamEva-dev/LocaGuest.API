using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;

public class GetAllContractsQueryHandler : IRequestHandler<GetAllContractsQuery, Result<List<ContractDto>>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetAllContractsQueryHandler> _logger;

    public GetAllContractsQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetAllContractsQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetAllContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _readDb.Contracts.AsNoTracking();

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
            var OccupantIds = contracts.Select(c => c.RenterOccupantId).Distinct().ToList();

            var properties = await _readDb.Properties.AsNoTracking()
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var tenants = await _readDb.Occupants.AsNoTracking()
                .Where(t => OccupantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.FullName, cancellationToken);

            var contractIds = contracts.Select(c => c.Id).ToList();

            var inventoryEntries = await _readDb.InventoryEntries.AsNoTracking()
                .Where(e => contractIds.Contains(e.ContractId))
                .ToDictionaryAsync(e => e.ContractId, e => e.Id, cancellationToken);

            var inventoryExits = await _readDb.InventoryExits.AsNoTracking()
                .Where(e => contractIds.Contains(e.ContractId))
                .ToDictionaryAsync(e => e.ContractId, e => e.Id, cancellationToken);

            var paymentsCounts = await _readDb.Payments.AsNoTracking()
                .Where(p => contractIds.Contains(p.ContractId))
                .GroupBy(p => p.ContractId)
                .Select(g => new { ContractId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ContractId, x => x.Count, cancellationToken);

            var result = contracts.Select(c => new ContractDto
            {
                Id = c.Id,
                Code = c.Code,
                PropertyId = c.PropertyId,
                OccupantId = c.RenterOccupantId,
                RoomId = c.RoomId,
                IsConflict = c.IsConflict,
                PropertyName = properties.GetValueOrDefault(c.PropertyId),
                OccupantName = tenants.GetValueOrDefault(c.RenterOccupantId),
                Type = c.Type.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.Rent,
                Charges = c.Charges,
                Deposit = c.Deposit,
                Status = c.Status.ToString(),
                Notes = c.Notes,
                PaymentsCount = paymentsCounts.GetValueOrDefault(c.Id),
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
                    (c.OccupantName?.ToLower().Contains(searchLower) ?? false)
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
