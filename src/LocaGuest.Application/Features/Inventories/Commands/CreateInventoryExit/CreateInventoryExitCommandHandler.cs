using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Inventories;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.CreateInventoryExit;

public class CreateInventoryExitCommandHandler : IRequestHandler<CreateInventoryExitCommand, Result<InventoryExitDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateInventoryExitCommandHandler> _logger;

    public CreateInventoryExitCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreateInventoryExitCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<InventoryExitDto>> Handle(CreateInventoryExitCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating inventory exit for contract {ContractId}", request.ContractId);

            // ✅ Validation: Contrat existe et est Active ou Terminated
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

            if (contract == null)
                return Result.Failure<InventoryExitDto>("Contract not found");

            if (contract.Status != ContractStatus.Active && contract.Status != ContractStatus.Terminated)
                return Result.Failure<InventoryExitDto>("Contract must be Active or Terminated to create inventory exit");

            // ✅ Validation: EDL entrée existe
            var inventoryEntry = await _context.InventoryEntries
                .Include(ie => ie.Items)
                .FirstOrDefaultAsync(ie => ie.Id == request.InventoryEntryId, cancellationToken);

            if (inventoryEntry == null)
                return Result.Failure<InventoryExitDto>("Inventory entry not found");

            // ✅ Validation: Un seul EDL sortie par contrat
            var existingExit = await _context.InventoryExits
                .FirstOrDefaultAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

            if (existingExit != null)
                return Result.Failure<InventoryExitDto>("An inventory exit already exists for this contract");

            // Créer l'entité domain
            var inventoryExit = InventoryExit.Create(
                propertyId: request.PropertyId,
                contractId: request.ContractId,
                renterTenantId: contract.RenterOccupantId,
                inventoryEntryId: request.InventoryEntryId,
                inspectionDate: request.InspectionDate,
                agentName: request.AgentName,
                tenantPresent: request.TenantPresent,
                roomId: request.RoomId,
                representativeName: request.RepresentativeName,
                generalObservations: request.GeneralObservations
            );

            // Ajouter les comparaisons
            foreach (var compDto in request.Comparisons)
            {
                var entryCondition = ParseCondition(compDto.EntryCondition);
                var exitCondition = ParseCondition(compDto.ExitCondition);
                
                var comparison = InventoryComparison.Create(
                    roomName: compDto.RoomName,
                    elementName: compDto.ElementName,
                    entryCondition: entryCondition,
                    exitCondition: exitCondition,
                    comment: compDto.Comment,
                    photoUrls: compDto.PhotoUrls
                );
                inventoryExit.AddComparison(comparison);
            }

            // Ajouter les dégradations
            foreach (var degDto in request.Degradations)
            {
                var degradation = Degradation.Create(
                    roomName: degDto.RoomName,
                    elementName: degDto.ElementName,
                    description: degDto.Description,
                    isImputedToTenant: degDto.IsImputedToTenant,
                    estimatedCost: degDto.EstimatedCost,
                    photoUrls: degDto.PhotoUrls
                );
                inventoryExit.AddDegradation(degradation);
            }

            // Ajouter les photos
            foreach (var photoUrl in request.PhotoUrls)
            {
                inventoryExit.AddPhoto(photoUrl);
            }

            // Ajouter les infos financières
            inventoryExit.SetFinancialInfo(request.OwnerCoveredAmount, request.FinancialNotes);

            // Marquer comme complété si comparaisons présentes
            if (request.Comparisons.Any())
            {
                inventoryExit.Complete();
            }

            // Sauvegarder
            _context.InventoryExits.Add(inventoryExit);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory exit created successfully: {InventoryId}, Deduction: {Amount}€", 
                inventoryExit.Id, inventoryExit.TotalDeductionAmount);

            // Mapper vers DTO
            var dto = new InventoryExitDto
            {
                Id = inventoryExit.Id,
                PropertyId = inventoryExit.PropertyId,
                RoomId = inventoryExit.RoomId,
                ContractId = inventoryExit.ContractId,
                RenterOccupantId = inventoryExit.RenterOccupantId,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory exit");
            return Result.Failure<InventoryExitDto>($"Error creating inventory exit: {ex.Message}");
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
