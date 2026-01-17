using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Inventories;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.CreateInventoryEntry;

public class CreateInventoryEntryCommandHandler : IRequestHandler<CreateInventoryEntryCommand, Result<InventoryEntryDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateInventoryEntryCommandHandler> _logger;

    public CreateInventoryEntryCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreateInventoryEntryCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<InventoryEntryDto>> Handle(CreateInventoryEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating inventory entry for contract {ContractId}", request.ContractId);

            // ✅ Validation: Contrat existe et est Signed
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

            if (contract == null)
                return Result.Failure<InventoryEntryDto>("Contract not found");

            if (contract.Status != ContractStatus.Signed && contract.Status != ContractStatus.Active)
                return Result.Failure<InventoryEntryDto>("Contract must be Signed or Active to create inventory entry");

            // ✅ Validation: Un seul EDL entrée par contrat
            var existingEntry = await _context.InventoryEntries
                .FirstOrDefaultAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

            if (existingEntry != null)
                return Result.Failure<InventoryEntryDto>("An inventory entry already exists for this contract");

            // ✅ Validation: Property existe
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);

            if (property == null)
                return Result.Failure<InventoryEntryDto>("Property not found");

            // Créer l'entité domain
            var inventoryEntry = InventoryEntry.Create(
                propertyId: request.PropertyId,
                contractId: request.ContractId,
                renterTenantId: contract.RenterOccupantId,
                inspectionDate: request.InspectionDate,
                agentName: request.AgentName,
                tenantPresent: request.TenantPresent,
                roomId: request.RoomId,
                representativeName: request.RepresentativeName,
                generalObservations: request.GeneralObservations
            );

            // Ajouter les items
            foreach (var itemDto in request.Items)
            {
                var condition = ParseCondition(itemDto.Condition);
                var item = InventoryItem.Create(
                    roomName: itemDto.RoomName,
                    elementName: itemDto.ElementName,
                    category: itemDto.Category,
                    condition: condition,
                    comment: itemDto.Comment,
                    photoUrls: itemDto.PhotoUrls
                );
                inventoryEntry.AddItem(item);
            }

            // Ajouter les photos
            foreach (var photoUrl in request.PhotoUrls)
            {
                inventoryEntry.AddPhoto(photoUrl);
            }

            // Marquer comme complété si items présents
            if (request.Items.Any())
            {
                inventoryEntry.Complete();
            }

            // Sauvegarder
            _context.InventoryEntries.Add(inventoryEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory entry created successfully: {InventoryId}", inventoryEntry.Id);

            // Mapper vers DTO
            var dto = new InventoryEntryDto
            {
                Id = inventoryEntry.Id,
                PropertyId = inventoryEntry.PropertyId,
                RoomId = inventoryEntry.RoomId,
                ContractId = inventoryEntry.ContractId,
                RenterOccupantId = inventoryEntry.RenterOccupantId,
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
                CreatedAt = inventoryEntry.CreatedAt
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory entry");
            return Result.Failure<InventoryEntryDto>($"Error creating inventory entry: {ex.Message}");
        }
    }

    private InventoryCondition ParseCondition(string condition)
    {
        return condition switch
        {
            "New" => InventoryCondition.New,
            "Good" => InventoryCondition.Good,
            "Fair" => InventoryCondition.Fair,
            "Poor" => InventoryCondition.Poor,
            "Damaged" => InventoryCondition.Damaged,
            _ => InventoryCondition.Good
        };
    }
}
