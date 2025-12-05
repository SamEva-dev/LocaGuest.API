using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;

public class CreateAddendumCommandHandler : IRequestHandler<CreateAddendumCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateAddendumCommandHandler> _logger;

    public CreateAddendumCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ILogger<CreateAddendumCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateAddendumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ========== 1. CHARGER ET VALIDER LE CONTRAT ==========
            
            var contract = await _unitOfWork.Contracts.Query()
                .Include(c => c.Addendums)
                .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

            if (contract == null)
                return Result.Failure<Guid>("Contract not found");

            // ========== 2. VALIDATIONS MÉTIER ==========
            
            // ✅ Le contrat doit être Active ou Signed
            if (!contract.CanCreateAddendum())
                return Result.Failure<Guid>(
                    $"Cannot create addendum for contract in status {contract.Status}. " +
                    "Only Active or Signed contracts can have addendums.");

            // ✅ Vérifier qu'il n'y a pas d'EDL de sortie
            var hasInventoryExit = await _context.InventoryExits
                .AnyAsync(ie => ie.ContractId == request.ContractId, cancellationToken);

            if (hasInventoryExit)
                return Result.Failure<Guid>("Cannot create addendum when exit inventory exists");

            // ✅ Vérifier la date d'effet
            if (request.EffectiveDate < DateTime.UtcNow.Date)
                return Result.Failure<Guid>("Effective date cannot be in the past");

            // ✅ Parser le type d'avenant
            if (!Enum.TryParse<AddendumType>(request.Type, true, out var addendumType))
                return Result.Failure<Guid>($"Invalid addendum type: {request.Type}");

            // ========== 3. VALIDATIONS SPÉCIFIQUES PAR TYPE ==========
            
            switch (addendumType)
            {
                case AddendumType.Financial:
                    if (!request.NewRent.HasValue && !request.NewCharges.HasValue)
                        return Result.Failure<Guid>("Financial addendum requires at least rent or charges modification");
                    
                    if (request.NewRent.HasValue && request.NewRent.Value <= 0)
                        return Result.Failure<Guid>("New rent must be positive");
                    
                    if (request.NewCharges.HasValue && request.NewCharges.Value < 0)
                        return Result.Failure<Guid>("New charges cannot be negative");
                    
                    // Vérifier augmentation excessive (warning)
                    if (request.NewRent.HasValue && request.NewRent.Value < contract.Rent)
                    {
                        _logger.LogWarning(
                            "Rent decrease in addendum from {OldRent}€ to {NewRent}€ for contract {ContractId}",
                            contract.Rent, request.NewRent.Value, contract.Id);
                    }
                    break;

                case AddendumType.Duration:
                    if (!request.NewEndDate.HasValue)
                        return Result.Failure<Guid>("Duration addendum requires new end date");
                    
                    if (request.NewEndDate.Value <= contract.StartDate)
                        return Result.Failure<Guid>("New end date must be after start date");
                    
                    if (request.NewEndDate.Value <= DateTime.UtcNow)
                        return Result.Failure<Guid>("New end date must be in the future");
                    
                    // Vérifier qu'il n'y a pas de contrat futur qui chevauche
                    var hasOverlap = await _unitOfWork.Contracts.Query()
                        .AnyAsync(c => 
                            c.PropertyId == contract.PropertyId &&
                            (contract.RoomId == null || c.RoomId == contract.RoomId) &&
                            c.Id != request.ContractId &&
                            c.Status != ContractStatus.Cancelled &&
                            c.StartDate < request.NewEndDate.Value,
                            cancellationToken);
                    
                    if (hasOverlap)
                        return Result.Failure<Guid>("New end date would overlap with another contract");
                    break;

                case AddendumType.Occupants:
                    if (string.IsNullOrWhiteSpace(request.OccupantChanges))
                        return Result.Failure<Guid>("Occupants addendum requires occupant changes details");
                    break;

                case AddendumType.Clauses:
                    if (string.IsNullOrWhiteSpace(request.NewClauses))
                        return Result.Failure<Guid>("Clauses addendum requires new clauses text");
                    
                    if (request.NewClauses.Length > 2000)
                        return Result.Failure<Guid>("Clauses text cannot exceed 2000 characters");
                    break;

                case AddendumType.Free:
                    if (string.IsNullOrWhiteSpace(request.Description))
                        return Result.Failure<Guid>("Free addendum requires description");
                    break;
            }

            // ✅ Vérifier changement de chambre si spécifié
            if (request.NewRoomId.HasValue)
            {
                // Vérifier que la chambre n'est pas déjà occupée
                var isRoomOccupied = await _unitOfWork.Contracts.Query()
                    .AnyAsync(c => 
                        c.PropertyId == contract.PropertyId &&
                        c.RoomId == request.NewRoomId.Value &&
                        c.Id != request.ContractId &&
                        (c.Status == ContractStatus.Active || c.Status == ContractStatus.Signed),
                        cancellationToken);
                
                if (isRoomOccupied)
                    return Result.Failure<Guid>("The target room is already occupied");
            }

            // ========== 4. CRÉER L'AVENANT ==========
            
            var addendum = Addendum.Create(
                contractId: request.ContractId,
                type: addendumType,
                effectiveDate: request.EffectiveDate,
                reason: request.Reason,
                description: request.Description
            );

            // ✅ Définir les modifications selon le type
            switch (addendumType)
            {
                case AddendumType.Financial:
                    addendum.SetFinancialChanges(
                        oldRent: contract.Rent,
                        newRent: request.NewRent ?? contract.Rent,
                        oldCharges: contract.Charges,
                        newCharges: request.NewCharges ?? contract.Charges
                    );
                    break;

                case AddendumType.Duration:
                    addendum.SetDurationChanges(
                        oldEndDate: contract.EndDate,
                        newEndDate: request.NewEndDate!.Value
                    );
                    break;

                case AddendumType.Occupants:
                    addendum.SetOccupantChanges(request.OccupantChanges!);
                    break;

                case AddendumType.Clauses:
                    addendum.SetClauseChanges(
                        oldClauses: contract.CustomClauses,
                        newClauses: request.NewClauses!
                    );
                    break;
            }

            // ✅ Changement de chambre si spécifié
            if (request.NewRoomId.HasValue)
            {
                addendum.SetRoomChanges(contract.RoomId, request.NewRoomId.Value);
            }

            // ✅ Attacher documents et notes
            if (request.AttachedDocumentIds.Any())
            {
                addendum.AttachDocuments(request.AttachedDocumentIds);
            }

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                addendum.AddNotes(request.Notes);
            }

            // ✅ Si pas de signature requise, marquer comme signé
            if (!request.RequireSignature)
            {
                addendum.MarkAsSigned();
            }

            // ========== 5. APPLIQUER LES MODIFICATIONS AU CONTRAT ==========
            
            // Si l'avenant est signé et la date d'effet est aujourd'hui, appliquer immédiatement
            if (addendum.SignatureStatus == AddendumSignatureStatus.Signed && 
                request.EffectiveDate.Date <= DateTime.UtcNow.Date)
            {
                switch (addendumType)
                {
                    case AddendumType.Financial:
                        if (request.NewRent.HasValue || request.NewCharges.HasValue)
                        {
                            contract.ApplyFinancialAddendum(
                                newRent: request.NewRent ?? contract.Rent,
                                newCharges: request.NewCharges ?? contract.Charges
                            );
                        }
                        break;

                    case AddendumType.Duration:
                        if (request.NewEndDate.HasValue)
                        {
                            contract.ApplyDurationAddendum(request.NewEndDate.Value);
                        }
                        break;

                    case AddendumType.Clauses:
                        if (!string.IsNullOrWhiteSpace(request.NewClauses))
                        {
                            contract.ApplyClausesAddendum(request.NewClauses);
                        }
                        break;
                }

                if (request.NewRoomId.HasValue)
                {
                    contract.ApplyRoomAddendum(request.NewRoomId.Value);
                }
            }

            // ========== 6. AJOUTER L'AVENANT AU CONTRAT ==========
            
            contract.AddAddendum(addendum);
            _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Addendum {AddendumId} of type {Type} created for contract {ContractId}",
                addendum.Id, addendumType, contract.Id);

            // TODO: Si SendEmail = true, envoyer notification au locataire

            return Result.Success(addendum.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating addendum for contract {ContractId}", request.ContractId);
            return Result.Failure<Guid>($"Error creating addendum: {ex.Message}");
        }
    }
}
