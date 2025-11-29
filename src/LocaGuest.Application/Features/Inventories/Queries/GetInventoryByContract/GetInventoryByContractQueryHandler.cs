using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryByContract;

public class GetInventoryByContractQueryHandler : IRequestHandler<GetInventoryByContractQuery, Result<ContractInventoriesDto>>
{
    private readonly ILocaGuestDbContext _context;

    public GetInventoryByContractQueryHandler(ILocaGuestDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ContractInventoriesDto>> Handle(GetInventoryByContractQuery request, CancellationToken cancellationToken)
    {
        var entry = await _context.InventoryEntries
            .Include(ie => ie.Items)
            .FirstOrDefaultAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

        var exit = await _context.InventoryExits
            .Include(ie => ie.Comparisons)
            .Include(ie => ie.Degradations)
            .FirstOrDefaultAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

        var result = new ContractInventoriesDto
        {
            Entry = entry != null ? MapEntryToDto(entry) : null,
            Exit = exit != null ? MapExitToDto(exit) : null
        };

        return Result.Success(result);
    }

    private InventoryEntryDto MapEntryToDto(Domain.Aggregates.InventoryAggregate.InventoryEntry entry)
    {
        return new InventoryEntryDto
        {
            Id = entry.Id,
            PropertyId = entry.PropertyId,
            RoomId = entry.RoomId,
            ContractId = entry.ContractId,
            TenantId = entry.TenantId,
            InspectionDate = entry.InspectionDate,
            AgentName = entry.AgentName,
            TenantPresent = entry.TenantPresent,
            RepresentativeName = entry.RepresentativeName,
            GeneralObservations = entry.GeneralObservations,
            Items = entry.Items.Select(i => new InventoryItemDto
            {
                RoomName = i.RoomName,
                ElementName = i.ElementName,
                Category = i.Category,
                Condition = i.Condition.ToString(),
                Comment = i.Comment,
                PhotoUrls = i.PhotoUrls.ToList()
            }).ToList(),
            PhotoUrls = entry.PhotoUrls.ToList(),
            Status = entry.Status.ToString(),
            CreatedAt = entry.CreatedAt
        };
    }

    private InventoryExitDto MapExitToDto(Domain.Aggregates.InventoryAggregate.InventoryExit exit)
    {
        return new InventoryExitDto
        {
            Id = exit.Id,
            PropertyId = exit.PropertyId,
            RoomId = exit.RoomId,
            ContractId = exit.ContractId,
            TenantId = exit.TenantId,
            InventoryEntryId = exit.InventoryEntryId,
            InspectionDate = exit.InspectionDate,
            AgentName = exit.AgentName,
            TenantPresent = exit.TenantPresent,
            RepresentativeName = exit.RepresentativeName,
            GeneralObservations = exit.GeneralObservations,
            Comparisons = exit.Comparisons.Select(c => new InventoryComparisonDto
            {
                RoomName = c.RoomName,
                ElementName = c.ElementName,
                EntryCondition = c.EntryCondition.ToString(),
                ExitCondition = c.ExitCondition.ToString(),
                HasDegradation = c.HasDegradation,
                Comment = c.Comment,
                PhotoUrls = c.PhotoUrls.ToList()
            }).ToList(),
            Degradations = exit.Degradations.Select(d => new DegradationDto
            {
                RoomName = d.RoomName,
                ElementName = d.ElementName,
                Description = d.Description,
                IsImputedToTenant = d.IsImputedToTenant,
                EstimatedCost = d.EstimatedCost,
                PhotoUrls = d.PhotoUrls.ToList()
            }).ToList(),
            PhotoUrls = exit.PhotoUrls.ToList(),
            TotalDeductionAmount = exit.TotalDeductionAmount,
            OwnerCoveredAmount = exit.OwnerCoveredAmount,
            FinancialNotes = exit.FinancialNotes,
            Status = exit.Status.ToString(),
            CreatedAt = exit.CreatedAt
        };
    }
}
