using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryEntry;

public class GetInventoryEntryQueryHandler : IRequestHandler<GetInventoryEntryQuery, Result<InventoryEntryDto>>
{
    private readonly ILocaGuestDbContext _context;

    public GetInventoryEntryQueryHandler(ILocaGuestDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InventoryEntryDto>> Handle(GetInventoryEntryQuery request, CancellationToken cancellationToken)
    {
        var inventoryEntry = await _context.InventoryEntries
            .Include(ie => ie.Items)
            .FirstOrDefaultAsync(ie => ie.Id == request.Id, cancellationToken);

        if (inventoryEntry == null)
            return Result.Failure<InventoryEntryDto>("Inventory entry not found");

        var dto = new InventoryEntryDto
        {
            Id = inventoryEntry.Id,
            PropertyId = inventoryEntry.PropertyId,
            RoomId = inventoryEntry.RoomId,
            ContractId = inventoryEntry.ContractId,
            RenterTenantId = inventoryEntry.RenterTenantId,
            InspectionDate = inventoryEntry.InspectionDate,
            AgentName = inventoryEntry.AgentName,
            TenantPresent = inventoryEntry.TenantPresent,
            RepresentativeName = inventoryEntry.RepresentativeName,
            GeneralObservations = inventoryEntry.GeneralObservations,
            Items = inventoryEntry.Items.Select(i => new InventoryItemDto
            {
                RoomName = i.RoomName,
                ElementName = i.ElementName,
                Category = i.Category,
                Condition = i.Condition.ToString(),
                Comment = i.Comment,
                PhotoUrls = i.PhotoUrls.ToList()
            }).ToList(),
            PhotoUrls = inventoryEntry.PhotoUrls.ToList(),
            Status = inventoryEntry.Status.ToString(),
            IsFinalized = inventoryEntry.IsFinalized,
            FinalizedAt = inventoryEntry.FinalizedAt,
            CreatedAt = inventoryEntry.CreatedAt
        };

        return Result.Success(dto);
    }
}
