using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.RenewContract;

public class RenewContractCommandHandler : IRequestHandler<RenewContractCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<RenewContractCommandHandler> _logger;

    public RenewContractCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ILogger<RenewContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RenewContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ========== 1. CHARGER ET VALIDER LE CONTRAT EXISTANT ==========
            
            var oldContract = await _unitOfWork.Contracts.Query()
                .Include(c => c.RequiredDocuments)
                .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

            if (oldContract == null)
                return Result.Failure<Guid>("Contract not found");

            // ========== 2. VALIDATIONS MÉTIER ==========
            
            // ✅ Le contrat doit être Active ou Expiring
            if (oldContract.Status != ContractStatus.Active && oldContract.Status != ContractStatus.Expiring)
                return Result.Failure<Guid>("Only active or expiring contracts can be renewed");

            // ✅ Vérifier qu'il ne reste pas beaucoup de temps (< 60 jours par défaut)
            var daysUntilEnd = (oldContract.EndDate.Date - DateTime.UtcNow.Date).TotalDays;
            if (daysUntilEnd > 60)
                return Result.Failure<Guid>($"Contract can only be renewed within 60 days of expiration (currently {daysUntilEnd} days remaining)");

            // ✅ Vérifier qu'il n'y a pas déjà un contrat futur
            var hasFutureContract = await _unitOfWork.Contracts.Query()
                .AnyAsync(c => 
                    c.PropertyId == oldContract.PropertyId &&
                    (oldContract.RoomId == null || c.RoomId == oldContract.RoomId) &&
                    c.StartDate > DateTime.UtcNow &&
                    c.Status != ContractStatus.Cancelled &&
                    c.Id != request.ContractId,
                    cancellationToken);

            if (hasFutureContract)
                return Result.Failure<Guid>("A future contract already exists for this property/room");

            // ✅ Vérifier qu'il n'y a pas d'EDL de sortie en cours
            var hasInventoryExit = await _context.InventoryExits
                .AnyAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

            if (hasInventoryExit)
                return Result.Failure<Guid>("Cannot renew a contract with an exit inventory (EDL sortie)");

            // ✅ Vérifier que la nouvelle date de début = lendemain de fin
            var expectedStartDate = oldContract.EndDate.Date.AddDays(1);
            if (request.NewStartDate.Date != expectedStartDate)
                return Result.Failure<Guid>($"New contract must start on {expectedStartDate:dd/MM/yyyy} (day after old contract ends)");

            // ✅ Vérifier cohérence des dates
            if (request.NewStartDate >= request.NewEndDate)
                return Result.Failure<Guid>("New start date must be before new end date");

            // ✅ Valider durée selon type de bail
            var duration = (request.NewEndDate - request.NewStartDate).TotalDays / 365.25;
            var contractType = Enum.Parse<ContractType>(request.ContractType, ignoreCase: true);
            
            var validDuration = contractType switch
            {
                ContractType.Furnished => duration >= 0.9 && duration <= 1.1, // 1 an ± 10%
                ContractType.Unfurnished => duration >= 2.8 && duration <= 3.2, // 3 ans ± 10%
                _ => true
            };

            if (!validDuration)
            {
                var expected = contractType == ContractType.Furnished ? "1 an" : "3 ans";
                return Result.Failure<Guid>($"Duration must be approximately {expected} for {contractType} contract");
            }

            // ✅ Valider révision de loyer (loi ALUR: max +3.5% dans zones tendues)
            if (request.NewRent < oldContract.Rent)
                return Result.Failure<Guid>("New rent cannot be lower than current rent");

            var rentIncrease = ((request.NewRent - oldContract.Rent) / oldContract.Rent) * 100;
            if (rentIncrease > 3.5m)
            {
                _logger.LogWarning("Rent increase of {Increase}% exceeds recommended 3.5% limit", rentIncrease);
                // Note: On log mais on ne bloque pas (peut être autorisé selon contexte)
            }

            // ✅ Valider charges positives
            if (request.NewCharges < 0)
                return Result.Failure<Guid>("Charges cannot be negative");

            // ✅ Vérifier absence de chevauchement avec autres contrats
            var hasOverlap = await _unitOfWork.Contracts.Query()
                .AnyAsync(c => 
                    c.PropertyId == oldContract.PropertyId &&
                    (oldContract.RoomId == null || c.RoomId == oldContract.RoomId) &&
                    c.Id != request.ContractId &&
                    c.Status != ContractStatus.Cancelled &&
                    c.Status != ContractStatus.Renewed &&
                    (
                        (c.StartDate <= request.NewEndDate && c.EndDate >= request.NewStartDate)
                    ),
                    cancellationToken);

            if (hasOverlap)
                return Result.Failure<Guid>("New contract dates overlap with an existing contract");

            // ========== 3. CRÉER LE NOUVEAU CONTRAT ==========
            
            var newContract = Contract.Create(
                propertyId: oldContract.PropertyId,
                renterTenantId: oldContract.RenterOccupantId,
                type: contractType,
                startDate: request.NewStartDate,
                endDate: request.NewEndDate,
                rent: request.NewRent,
                charges: request.NewCharges,
                deposit: request.Deposit ?? oldContract.Deposit,
                roomId: oldContract.RoomId
            );

            // ✅ Mettre à jour les clauses et IRL
            newContract.UpdateCustomClauses(request.CustomClauses);
            newContract.UpdateIRL(request.PreviousIRL, request.CurrentIRL);
            
            // Note: Les notes sont passées via UpdateBasicInfo si besoin
            // Pour l'instant le nouveau contrat hérite les propriétés de base via Create()

            // ✅ Associer les documents joints
            foreach (var docId in request.AttachedDocumentIds)
            {
                // Note: Il faudrait vérifier le type de document via DocumentsApi
                // Pour l'instant on associe directement
                newContract.MarkDocumentProvided(DocumentType.Bail);
            }

            // ========== 4. CLÔTURER L'ANCIEN CONTRAT ==========
            
            oldContract.MarkAsRenewed(newContract.Id);

            // ========== 5. SAUVEGARDER ==========
            
            await _unitOfWork.Contracts.AddAsync(newContract, cancellationToken);
            _unitOfWork.Contracts.Update(oldContract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Contract {OldContractId} renewed successfully. New contract: {NewContractId}. Rent: {OldRent}€ → {NewRent}€",
                oldContract.Id,
                newContract.Id,
                oldContract.Rent,
                request.NewRent
            );

            return Result.Success(newContract.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing contract {ContractId}", request.ContractId);
            return Result.Failure<Guid>($"Error renewing contract: {ex.Message}");
        }
    }
}
