using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryExit;

public class GetInventoryExitQueryHandler : IRequestHandler<GetInventoryExitQuery, Result<InventoryExitDto>>
{
    private readonly ILocaGuestDbContext _context;

    public GetInventoryExitQueryHandler(ILocaGuestDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InventoryExitDto>> Handle(GetInventoryExitQuery request, CancellationToken cancellationToken)
    {
        var inventoryExit = await _context.InventoryExits
            .Include(ie => ie.Comparisons)
            .Include(ie => ie.Degradations)
            .FirstOrDefaultAsync(ie => ie.Id == request.Id, cancellationToken);

        if (inventoryExit == null)
            return Result.Failure<InventoryExitDto>("Inventory exit not found");

        var dto = new InventoryExitDto
        {
            Id = inventoryExit.Id,
            PropertyId = inventoryExit.PropertyId,
            RoomId = inventoryExit.RoomId,
            ContractId = inventoryExit.ContractId,
            TenantId = inventoryExit.TenantId,
            InventoryEntryId = inventoryExit.InventoryEntryId,
            InspectionDate = inventoryExit.InspectionDate,
            AgentName = inventoryExit.AgentName,
            TenantPresent = inventoryExit.TenantPresent,
            RepresentativeName = inventoryExit.RepresentativeName,
            GeneralObservations = inventoryExit.GeneralObservations,
            Comparisons = inventoryExit.Comparisons.Select(c => new InventoryComparisonDto
            {
                RoomName = c.RoomName,
                ElementName = c.ElementName,
                EntryCondition = c.EntryCondition.ToString(),
                ExitCondition = c.ExitCondition.ToString(),
                HasDegradation = c.HasDegradation,
                Comment = c.Comment,
                PhotoUrls = c.PhotoUrls.ToList()
            }).ToList(),
            Degradations = inventoryExit.Degradations.Select(d => new DegradationDto
            {
                RoomName = d.RoomName,
                ElementName = d.ElementName,
                Description = d.Description,
                IsImputedToTenant = d.IsImputedToTenant,
                EstimatedCost = d.EstimatedCost,
                PhotoUrls = d.PhotoUrls.ToList()
            }).ToList(),
            PhotoUrls = inventoryExit.PhotoUrls.ToList(),
            TotalDeductionAmount = inventoryExit.TotalDeductionAmount,
            OwnerCoveredAmount = inventoryExit.OwnerCoveredAmount,
            FinancialNotes = inventoryExit.FinancialNotes,
            Status = inventoryExit.Status.ToString(),
            CreatedAt = inventoryExit.CreatedAt
        };

        return Result.Success(dto);
    }
}
