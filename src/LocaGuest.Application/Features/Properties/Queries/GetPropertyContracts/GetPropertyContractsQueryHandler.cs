using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;

public class GetPropertyContractsQueryHandler : IRequestHandler<GetPropertyContractsQuery, Result<List<ContractDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetPropertyContractsQueryHandler> _logger;

    public GetPropertyContractsQueryHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ILogger<GetPropertyContractsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetPropertyContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            var paymentCountsByContract = await _unitOfWork.Payments.Query()
                .Where(p => p.PropertyId == propertyId)
                .GroupBy(p => p.ContractId)
                .Select(g => new { ContractId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ContractId, x => x.Count, cancellationToken);

            var contracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.PropertyId == propertyId)
                .OrderByDescending(c => c.StartDate)
                .Select(c => new ContractDto
                {
                    Id = c.Id,
                    PropertyId = c.PropertyId,
                    OccupantId = c.RenterOccupantId,
                    Type = c.Type.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Rent = c.Rent,
                    Deposit = c.Deposit,
                    Status = c.Status.ToString(),
                    PaymentsCount = 0,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // Charger les noms des locataires
            var OccupantIds = contracts.Select(c => c.OccupantId).Distinct().ToList();
            var tenants = await _unitOfWork.Occupants.Query()
                .Where(t => OccupantIds.Contains(t.Id))
                .Select(t => new { t.Id, t.FullName })
                .ToListAsync(cancellationToken);
            
            // ✅ Charger les informations EDL pour chaque contrat
            var contractIds = contracts.Select(c => c.Id).ToList();
            var inventoryEntries = await _context.InventoryEntries
                .Where(ie => contractIds.Contains(ie.ContractId))
                .Select(ie => new { ie.Id, ie.ContractId, ie.IsFinalized })
                .ToListAsync(cancellationToken);
                
            var inventoryExits = await _context.InventoryExits
                .Where(ie => contractIds.Contains(ie.ContractId))
                .Select(ie => new { ie.Id, ie.ContractId, ie.IsFinalized })
                .ToListAsync(cancellationToken);

            foreach (var contract in contracts)
            {
                contract.OccupantName = tenants.FirstOrDefault(t => t.Id == contract.OccupantId)?.FullName;

                contract.PaymentsCount = paymentCountsByContract.GetValueOrDefault(contract.Id);
                
                // ✅ Enrichir avec les informations EDL
                var entry = inventoryEntries.FirstOrDefault(ie => ie.ContractId == contract.Id);
                var exit = inventoryExits.FirstOrDefault(ie => ie.ContractId == contract.Id);
                
                contract.HasInventoryEntry = entry?.IsFinalized == true;
                contract.InventoryEntryId = entry?.Id;
                contract.HasInventoryExit = exit?.IsFinalized == true;
                contract.InventoryExitId = exit?.Id;
            }

            return Result.Success(contracts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<ContractDto>>("Error retrieving property contracts");
        }
    }
}
