using LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;
using LocaGuest.Application.Features.Contracts.Queries.GetContractStats;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly LocaGuestDbContext _context;
    private readonly ILogger<ContractsController> _logger;
    private readonly IMediator _mediator;

    public ContractsController(
        LocaGuestDbContext context, 
        ILogger<ContractsController> logger,
        IMediator mediator)
    {
        _context = context;
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetContractStats()
    {
        var query = new GetContractStatsQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllContracts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var query = new GetAllContractsQuery
        {
            SearchTerm = searchTerm,
            Status = status,
            Type = type
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetContracts(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Contracts.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Aggregates.ContractAggregate.ContractStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        var total = await query.CountAsync();
        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                PropertyName = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Name).FirstOrDefault(),
                TenantId = c.RenterTenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.RenterTenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = contracts });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var contract = await _context.Contracts
            .Include(c => c.Payments)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.PropertyId,
                PropertyName = _context.Properties.Where(p => p.Id == c.PropertyId).Select(p => p.Name).FirstOrDefault(),
                TenantId = c.RenterTenantId,
                TenantName = _context.Tenants.Where(t => t.Id == c.RenterTenantId).Select(t => t.FullName).FirstOrDefault(),
                c.Type,
                c.StartDate,
                c.EndDate,
                c.Rent,
                c.Deposit,
                c.Status,
                c.Notes,
                PaymentsCount = c.Payments.Count
            })
            .FirstOrDefaultAsync();

        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        return Ok(contract);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        // Vérifier que la propriété et le locataire existent
        var property = await _context.Properties.FindAsync(request.PropertyId);
        if (property == null)
            return BadRequest(new { message = "Property not found" });

        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null)
            return BadRequest(new { message = "Tenant not found" });

        var contract = Domain.Aggregates.ContractAggregate.Contract.Create(
            request.PropertyId,
            request.TenantId,
            request.Type,
            request.StartDate,
            request.EndDate,
            request.Rent,
            request.Charges,
            request.Deposit,
            request.RoomId
        );

        // NOTE: La propriété sera marquée comme occupée lors de la signature du contrat
        // Un contrat Draft ne rend pas la propriété occupée

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        // ✅ FIX #1: Return tenantName in response
        return CreatedAtAction(nameof(GetContract), new { id = contract.Id }, new 
        { 
            id = contract.Id, 
            propertyId = contract.PropertyId, 
            propertyName = property.Name,
            tenantId = contract.RenterTenantId,
            tenantName = tenant.FullName, // ✅ Include tenant name
            type = contract.Type,
            startDate = contract.StartDate,
            endDate = contract.EndDate,
            rent = contract.Rent,
            charges = contract.Charges,
            deposit = contract.Deposit,
            status = contract.Status,
            roomId = contract.RoomId
        });
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var contract = await _context.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        var payment = contract.RecordPayment(
            request.Amount,
            request.PaymentDate,
            request.Method
        );

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Payment recorded successfully", 
            paymentId = payment.Id,
            amount = payment.Amount,
            date = payment.PaymentDate
        });
    }

    [HttpPut("{id:guid}/terminate")]
    public async Task<IActionResult> TerminateContract(Guid id, [FromBody] TerminateContractRequest request)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        contract.Terminate(request.TerminationDate);

        // Si demandé, marquer la propriété comme vacante
        if (request.MarkPropertyVacant)
        {
            var property = await _context.Properties.FindAsync(contract.PropertyId);
            if (property != null)
            {
                property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Vacant);
            }
        }
        
        // ✅ DISSOCIATION: Retirer le locataire du bien
        var tenant = await _context.Tenants.FindAsync(contract.RenterTenantId);
        if (tenant != null)
        {
            tenant.DissociateFromProperty();
            tenant.Deactivate();
            _logger.LogInformation(
                "Tenant {TenantCode} dissociated from property (contract terminated)",
                tenant.Code);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Contract terminated successfully", id = contract.Id });
    }

    [HttpPut("{id:guid}/mark-signed")]
    public async Task<IActionResult> MarkContractAsSigned(Guid id, [FromBody] MarkAsSignedRequest? request = null)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        var property = await _context.Properties.FindAsync(contract.PropertyId);
        if (property == null)
            return NotFound(new { message = "Property not found" });
            
        var tenant = await _context.Tenants.FindAsync(contract.RenterTenantId);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        try
        {
            // ⭐ VALIDATION MÉTIER: Vérifier selon le type de bien
            var existingSignedContracts = await _context.Contracts
                .Where(c => 
                    c.PropertyId == contract.PropertyId &&
                    c.Id != id &&
                    (c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Signed ||
                     c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active))
                .ToListAsync();
            
            // Non-colocation ou Colocation solidaire: 1 seul contrat Signed/Active autorisé
            if (property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.Complete ||
                property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationSolidaire)
            {
                if (existingSignedContracts.Any())
                {
                    return BadRequest(new { 
                        message = "Un contrat signé ou actif existe déjà pour ce bien. " +
                                 "Un seul contrat est autorisé pour une location complète ou colocation solidaire."
                    });
                }
            }
            
            // Colocation individuelle: vérifier chambre disponible
            if (property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationIndividual ||
                property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.Colocation)
            {
                if (contract.RoomId.HasValue)
                {
                    var roomTaken = existingSignedContracts.Any(c => c.RoomId == contract.RoomId);
                    if (roomTaken)
                    {
                        return BadRequest(new { 
                            message = "Cette chambre est déjà occupée ou réservée par un autre contrat."
                        });
                    }
                }
            }
            
            // Marquer le contrat comme signé
            contract.MarkAsSigned(request?.SignedDate);
            
            // 1. Annuler tous les autres contrats Draft ou Pending du même locataire
            // et marquer comme conflictuels
            var otherContractsToCancel = await _context.Contracts
                .Where(c => 
                    c.RenterTenantId == contract.RenterTenantId &&
                    c.Id != id &&
                    (c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Draft || 
                     c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Pending))
                .ToListAsync();
            
            foreach (var otherContract in otherContractsToCancel)
            {
                otherContract.MarkAsConflict();
                _logger.LogInformation("Contract {ContractId} marked as conflict because tenant chose contract {ChosenContractId}", 
                    otherContract.Id, id);
            }
            
            // 2. Marquer la propriété comme RESERVED (pas Occupied - commence à StartDate)
            property.SetReserved(contract.Id, contract.StartDate);
            _logger.LogInformation("Property {PropertyId} marked as Reserved for contract {ContractId}", 
                contract.PropertyId, contract.Id);
            
            // 3. Marquer le locataire comme Reserved
            tenant.SetReserved(contract.Id, contract.StartDate);
            _logger.LogInformation("Tenant {TenantId} marked as Reserved for contract {ContractId}", 
                contract.RenterTenantId, contract.Id);
            
            // 4. ✅ ASSIGNATION AUTOMATIQUE: Assigner le locataire au bien (Futur occupant)
            tenant.AssociateToProperty(property.Id, property.Code);
            _logger.LogInformation(
                "Tenant {TenantCode} automatically assigned to Property {PropertyCode} as future occupant",
                tenant.Code, property.Code);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Contract {ContractId} marked as Signed, {CancelledCount} other contracts marked as conflict, " +
                "property and tenant marked as Reserved", 
                id, otherContractsToCancel.Count);
                
            return Ok(new { 
                message = "Contrat marqué comme signé avec succès. Le bien et le locataire sont maintenant réservés.", 
                id = contract.Id,
                conflictContracts = otherContractsToCancel.Count,
                propertyStatus = "Reserved",
                tenantStatus = "Reserved",
                startDate = contract.StartDate
            });
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Activer manuellement un contrat signé (normalement fait automatiquement par le BackgroundService)
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> ActivateContract(Guid id)
    {
        var contract = await _context.Contracts.FindAsync(id);
            
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        try
        {
            // Activer le contrat
            contract.Activate();

            // Charger le bien associé
            var property = await _context.Properties.FindAsync(contract.PropertyId);
            if (property != null)
            {
                if (property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationIndividual
                    || property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.Colocation)
                {
                    property.IncrementOccupiedRooms();
                    
                    if (property.OccupiedRooms >= (property.TotalRooms ?? 0))
                    {
                        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Occupied);
                    }
                    else
                    {
                        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.PartiallyOccupied);
                    }
                }
                else
                {
                    property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Occupied);
                }
            }

            // Charger le locataire associé
            var tenant = await _context.Tenants.FindAsync(contract.RenterTenantId);
            if (tenant != null)
            {
                tenant.SetActive();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Contrat {ContractId} activé manuellement (Signed → Active)",
                id);
                
            return Ok(new { 
                message = "Contrat activé avec succès", 
                id = contract.Id,
                propertyStatus = property?.Status.ToString(),
                tenantStatus = tenant?.Status.ToString()
            });
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'activation du contrat {ContractId}", id);
            return StatusCode(500, new { message = "Erreur lors de l'activation du contrat" });
        }
    }
    
    /// <summary>
    /// Marquer manuellement un contrat comme expiré (normalement fait automatiquement par le BackgroundService)
    /// </summary>
    [HttpPut("{id:guid}/mark-expired")]
    public async Task<IActionResult> MarkContractAsExpired(Guid id)
    {
        var contract = await _context.Contracts.FindAsync(id);
            
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        try
        {
            // Marquer comme expiré
            contract.MarkAsExpired();

            // Charger le bien associé
            var property = await _context.Properties.FindAsync(contract.PropertyId);
            if (property != null)
            {
                if (property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationIndividual || 
                    property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.Colocation)
                {
                    property.DecrementOccupiedRooms();
                    
                    if (property.OccupiedRooms == 0)
                    {
                        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Vacant);
                    }
                    else
                    {
                        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.PartiallyOccupied);
                    }
                }
                else
                {
                    // Vérifier s'il reste d'autres contrats actifs
                    var hasOtherActiveContracts = await _context.Contracts
                        .AnyAsync(c => 
                            c.PropertyId == property.Id &&
                            c.Id != id &&
                            c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active);

                    if (!hasOtherActiveContracts)
                    {
                        property.SetStatus(Domain.Aggregates.PropertyAggregate.PropertyStatus.Vacant);
                    }
                }
            }

            // Charger le locataire associé
            var tenant = await _context.Tenants.FindAsync(contract.RenterTenantId);
            if (tenant != null)
            {
                // Vérifier si le locataire a d'autres contrats actifs
                var hasOtherActiveContracts = await _context.Contracts
                    .AnyAsync(c => 
                        c.RenterTenantId == tenant.Id &&
                        c.Id != id &&
                        c.Status == Domain.Aggregates.ContractAggregate.ContractStatus.Active);

                if (!hasOtherActiveContracts)
                {
                    // ✅ DISSOCIATION: Retirer le locataire du bien
                    tenant.DissociateFromProperty();
                    tenant.Deactivate();
                    _logger.LogInformation(
                        "Tenant {TenantCode} dissociated from property (contract expired)",
                        tenant.Code);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Contrat {ContractId} marqué comme expiré manuellement (Active → Expired)",
                id);
                
            return Ok(new { 
                message = "Contrat marqué comme expiré", 
                id = contract.Id,
                propertyStatus = property?.Status.ToString(),
                tenantStatus = tenant?.Status.ToString()
            });
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'expiration du contrat {ContractId}", id);
            return StatusCode(500, new { message = "Erreur lors de l'expiration du contrat" });
        }
    }
    
    /// <summary>
    /// Supprimer un contrat (uniquement les contrats Draft ou Cancelled)
    /// DELETE /api/contracts/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteContract(Guid id)
    {
        var contract = await _context.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (contract == null)
            return NotFound(new { message = "Contract not found" });

        // ✅ VALIDATION: Seuls les contrats Draft ou Cancelled peuvent être supprimés
        if (contract.Status != Domain.Aggregates.ContractAggregate.ContractStatus.Draft &&
            contract.Status != Domain.Aggregates.ContractAggregate.ContractStatus.Cancelled)
        {
            return BadRequest(new { 
                message = $"Impossible de supprimer un contrat avec le statut '{contract.Status}'. Seuls les contrats Draft ou Cancelled peuvent être supprimés."
            });
        }

        try
        {
            // ✅ CASCADE: Supprimer les paiements associés
            if (contract.Payments.Any())
            {
                _context.Payments.RemoveRange(contract.Payments);
                _logger.LogInformation(
                    "Suppression de {PaymentCount} paiements pour le contrat {ContractId}",
                    contract.Payments.Count, id);
            }

            // ✅ CASCADE: Supprimer les documents associés
            var documents = await _context.Documents
                .Where(d => d.ContractId == id)
                .ToListAsync();
                
            if (documents.Any())
            {
                _context.Documents.RemoveRange(documents);
                _logger.LogInformation(
                    "Suppression de {DocumentCount} documents pour le contrat {ContractId}",
                    documents.Count, id);
            }

            // Supprimer le contrat
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Contrat {ContractId} (Code: {ContractCode}, Status: {Status}) supprimé avec succès",
                id, contract.Code, contract.Status);
                
            return Ok(new { 
                message = "Contrat supprimé avec succès", 
                id = contract.Id,
                deletedPayments = contract.Payments.Count,
                deletedDocuments = documents.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la suppression du contrat {ContractId}", id);
            return StatusCode(500, new { message = "Erreur lors de la suppression du contrat", error = ex.Message });
        }
    }
}

public record CreateContractRequest(
    Guid PropertyId,
    Guid TenantId,
    Domain.Aggregates.ContractAggregate.ContractType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Rent,
    decimal Charges = 0,
    decimal? Deposit = null,
    Guid? RoomId = null
);

public record RecordPaymentRequest(
    decimal Amount,
    DateTime PaymentDate,
    Domain.Aggregates.ContractAggregate.PaymentMethod Method
);

public record TerminateContractRequest(
    DateTime TerminationDate,
    bool MarkPropertyVacant = true
);

public record MarkAsSignedRequest(
    DateTime? SignedDate = null
);
