using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.FinalizeInventoryEntry;

public class FinalizeInventoryEntryCommandHandler : IRequestHandler<FinalizeInventoryEntryCommand, Result>
{
    private readonly ILocaGuestDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FinalizeInventoryEntryCommandHandler> _logger;

    public FinalizeInventoryEntryCommandHandler(
        ILocaGuestDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<FinalizeInventoryEntryCommandHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(FinalizeInventoryEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Récupérer l'EDL
            var inventory = await _context.InventoryEntries
                .FirstOrDefaultAsync(ie => ie.Id == request.InventoryEntryId, cancellationToken);
                
            if (inventory == null)
                return Result.Failure("Inventory entry not found");

            // Finaliser l'EDL
            inventory.MarkAsFinalized();

            // Récupérer le contrat associé
            var contract = await _unitOfWork.Contracts.GetByIdAsync(inventory.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Associated contract not found");

            // ✅ Si le contrat est Signed et que la date de début est aujourd'hui ou passée → Activer
            if (contract.Status == ContractStatus.Signed && contract.StartDate.Date <= DateTime.UtcNow.Date)
            {
                // Le contrat devient Active
                contract.Activate();
                
                // TODO: Mettre à jour le statut du bien (Occupé/Partiellement occupé)
                // TODO: Mettre à jour le statut du locataire (Occupant)
                
                _logger.LogInformation(
                    "Contract {ContractId} automatically activated after EDL entry finalization",
                    contract.Id);
            }

            // Les entités sont déjà trackées par le DbContext (chargées via EF / repository).
            // Ne pas appeler Update() ici: cela marque OrganizationId comme modifié et déclenche
            // la protection "OrganizationId cannot be modified after entity creation.".
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Inventory entry {InventoryId} finalized successfully", request.InventoryEntryId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing inventory entry {InventoryId}", request.InventoryEntryId);
            return Result.Failure($"Error finalizing inventory: {ex.Message}");
        }
    }
}
