using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;

public class CreateAddendumCommandHandler : IRequestHandler<CreateAddendumCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly IOrganizationContext _orgContext;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateAddendumCommandHandler> _logger;

    public CreateAddendumCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        IOrganizationContext orgContext,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        IMediator mediator,
        ILogger<CreateAddendumCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _orgContext = orgContext;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateAddendumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ========== 1. CHARGER ET VALIDER LE CONTRAT ==========
            
            var contract = await _unitOfWork.Contracts.Query()
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
                    if (request.EffectiveDate.Day != 1)
                        return Result.Failure<Guid>("Financial addendum effective date must be the 1st of the month");

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
                    if (request.EffectiveDate.Day != 1)
                        return Result.Failure<Guid>("Occupants addendum effective date must be the 1st of the month");

                    if (!request.OccupantChanges.HasValue
                        || request.OccupantChanges.Value.ValueKind == JsonValueKind.Undefined
                        || request.OccupantChanges.Value.ValueKind == JsonValueKind.Null)
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
                    addendum.SetOccupantChanges(JsonSerializer.Serialize(request.OccupantChanges!.Value));
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
            var attachedDocuments = request.AttachedDocumentIds?.ToList() ?? new List<Guid>();
            if (attachedDocuments.Any())
            {
                addendum.AttachDocuments(attachedDocuments.Distinct().ToList());
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

            // ========== 6. AJOUTER L'AVENANT AU CONTRAT ==========

            await _unitOfWork.Addendums.AddAsync(addendum, cancellationToken);

            if (!request.RequireSignature
                && addendum.Type == AddendumType.Occupants
                )
            {
                var applyResult = await ApplyOccupantsChangesIfAnyAsync(addendum, cancellationToken);
                if (!applyResult.IsSuccess)
                    return Result.Failure<Guid>(applyResult.ErrorMessage ?? "Unable to apply occupants changes");
            }

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

    private async Task<Result> ApplyOccupantsChangesIfAnyAsync(Addendum addendum, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(addendum.OccupantChanges))
            return Result.Failure("Occupants addendum requires occupant changes details");

        OccupantChangesDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OccupantChangesDto>(
                addendum.OccupantChanges,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            payload = null;
        }

        if (payload?.Participants == null || payload.Participants.Count == 0)
            return Result.Failure("Occupants addendum requires occupant changes details");

        if (!Enum.TryParse<BillingShareType>(payload.SplitType ?? string.Empty, true, out var shareType))
            return Result.Failure("Invalid splitType for occupants addendum");

        if (payload.Participants.Any(p => p == null || p.OccupantId == Guid.Empty))
            return Result.Failure("Invalid OccupantId in occupants addendum");

        var distinct = payload.Participants
            .GroupBy(p => p.OccupantId)
            .Select(g => g.First())
            .ToList();

        if (distinct.Count != payload.Participants.Count)
            return Result.Failure("Duplicate OccupantId in occupants addendum");

        if (distinct.Any(p => p.ShareValue < 0m))
            return Result.Failure("ShareValue cannot be negative");

        var shareSum = distinct.Sum(p => p.ShareValue);

        if (shareType == BillingShareType.Percentage)
        {
            if (Math.Abs(shareSum - 100m) > 0.0001m)
                return Result.Failure("Percentage split must total 100");
        }
        else
        {
            var stateResult = await _effectiveContractStateResolver.ResolveAsync(addendum.ContractId, addendum.EffectiveDate, cancellationToken);
            if (!stateResult.IsSuccess || stateResult.Data == null)
                return Result.Failure(stateResult.ErrorMessage ?? "Unable to resolve effective contract state");

            var expected = stateResult.Data.Rent + stateResult.Data.Charges;
            if (Math.Abs(shareSum - expected) > 0.01m)
                return Result.Failure("FixedAmount split must total (Rent + Charges)");
        }

        var effectiveDate = addendum.EffectiveDate;
        var endDate = effectiveDate.AddDays(-1);

        var existing = await _unitOfWork.ContractParticipants
            .GetEffectiveByContractIdAtDateAsync(addendum.ContractId, effectiveDate, cancellationToken);

        foreach (var p in existing)
        {
            p.EndAt(endDate);
            _unitOfWork.ContractParticipants.Update(p);
        }

        foreach (var np in distinct)
        {
            var created = ContractParticipant.Create(
                contractId: addendum.ContractId,
                tenantId: np.OccupantId,
                startDate: effectiveDate,
                endDate: null,
                shareType: shareType,
                shareValue: np.ShareValue);

            await _unitOfWork.ContractParticipants.AddAsync(created, cancellationToken);
        }

        return Result.Success();
    }

    private sealed record OccupantChangesDto(string? SplitType, List<OccupantParticipantDto> Participants);
    private sealed record OccupantParticipantDto(Guid OccupantId, decimal ShareValue);
}
