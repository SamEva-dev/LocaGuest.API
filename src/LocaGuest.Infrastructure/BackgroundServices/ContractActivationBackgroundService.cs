using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.BackgroundServices;

/// <summary>
/// Service d'arrière-plan pour activer automatiquement les contrats signés
/// dont la date de début est atteinte
/// Exécution: Toutes les heures
/// </summary>
public class ContractActivationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContractActivationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Toutes les heures

    private static bool IsColocation(PropertyUsageType usageType)
        => usageType == PropertyUsageType.ColocationIndividual || usageType == PropertyUsageType.Colocation || usageType == PropertyUsageType.ColocationSolidaire;

    public ContractActivationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ContractActivationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 ContractActivationBackgroundService démarré");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessContractActivationsAsync(stoppingToken);
                await ProcessContractExpirationsAsync(stoppingToken);
                await ProcessRoomOnHoldExpirationsAsync(stoppingToken);
                
                _logger.LogDebug("⏰ Prochain cycle dans {Interval}", _interval);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "❌ Erreur lors du traitement des contrats");
                // Attendre 5 minutes avant de réessayer en cas d'erreur
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 ContractActivationBackgroundService arrêté");
    }

    private async Task ProcessContractActivationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();

        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        // Récupérer les contrats Signed dont la date de début est aujourd'hui ou passée
        var contractsToActivate = await context.Contracts
            .Where(c => 
                c.Status == ContractStatus.Signed &&
                c.StartDate.Date <= today)
            .ToListAsync(cancellationToken);

        if (!contractsToActivate.Any())
        {
            _logger.LogDebug("✅ Aucun contrat à activer");
            return;
        }

        _logger.LogInformation("📋 {Count} contrat(s) à activer", contractsToActivate.Count);

        foreach (var contract in contractsToActivate)
        {
            try
            {
                // Activer le contrat
                contract.Activate();

                await UpdatePropertyStatusForActivatedContractAsync(
                    context,
                    contract,
                    cancellationToken);

                // Charger le locataire associé
                var tenant = await context.Tenants.FindAsync(contract.RenterTenantId);
                if (tenant != null)
                {
                    tenant.SetActive();
                    _logger.LogInformation(
                        "✅ Locataire {TenantCode} mis à jour → Active",
                        tenant.Code);
                }

                _logger.LogInformation(
                    "✅ Contrat {ContractCode} activé avec succès (Signed → Active)",
                    contract.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Erreur lors de l'activation du contrat {ContractCode}",
                    contract.Code);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "✅ Activation terminée: {Count} contrat(s) activé(s)",
            contractsToActivate.Count);
    }

    private async Task ProcessContractExpirationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();

        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        // Récupérer les contrats Active dont la date de fin est passée
        var contractsToExpire = await context.Contracts
            .Where(c => 
                c.Status == ContractStatus.Active &&
                c.EndDate.Date < today)
            .ToListAsync(cancellationToken);

        if (!contractsToExpire.Any())
        {
            _logger.LogDebug("✅ Aucun contrat à expirer");
            return;
        }

        _logger.LogInformation("📋 {Count} contrat(s) à expirer", contractsToExpire.Count);

        foreach (var contract in contractsToExpire)
        {
            try
            {
                // Marquer comme expiré
                contract.MarkAsExpired();

                await UpdatePropertyStatusForExpiredContractAsync(
                    context,
                    contract,
                    cancellationToken);

                // Charger le locataire associé
                var tenant = await context.Tenants.FindAsync(contract.RenterTenantId);
                if (tenant != null)
                {
                    // Vérifier si le locataire a d'autres contrats actifs
                    var hasOtherActiveContracts = await context.Contracts
                        .AnyAsync(c => 
                            c.RenterTenantId == tenant.Id &&
                            c.Id != contract.Id &&
                            c.Status == ContractStatus.Active,
                            cancellationToken);

                    if (!hasOtherActiveContracts)
                    {
                        // ✅ DISSOCIATION: Retirer le locataire du bien
                        tenant.DissociateFromProperty();
                        tenant.Deactivate();
                        _logger.LogInformation(
                            "✅ Locataire {TenantCode} dissocié du bien et désactivé (contrat expiré)",
                            tenant.Code);
                    }
                }

                _logger.LogInformation(
                    "✅ Contrat {ContractCode} expiré (Active → Expired)",
                    contract.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Erreur lors de l'expiration du contrat {ContractCode}",
                    contract.Code);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "✅ Expiration terminée: {Count} contrat(s) expiré(s)",
            contractsToExpire.Count);
    }

    private async Task ProcessRoomOnHoldExpirationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();

        var now = DateTime.UtcNow;

        var propertiesWithExpiredOnHoldRooms = await context.Properties
            .Include(p => p.Rooms)
            .Where(p => p.Rooms.Any(r =>
                r.Status == PropertyRoomStatus.OnHold &&
                r.OnHoldUntilUtc.HasValue &&
                r.OnHoldUntilUtc.Value < now))
            .ToListAsync(cancellationToken);

        if (!propertiesWithExpiredOnHoldRooms.Any())
        {
            _logger.LogDebug("✅ Aucune chambre OnHold expirée");
            return;
        }

        var releasedRooms = 0;
        var deletedDraftContracts = 0;

        foreach (var property in propertiesWithExpiredOnHoldRooms)
        {
            var expiredRooms = property.Rooms
                .Where(r =>
                    r.Status == PropertyRoomStatus.OnHold &&
                    r.OnHoldUntilUtc.HasValue &&
                    r.OnHoldUntilUtc.Value < now)
                .ToList();

            foreach (var room in expiredRooms)
            {
                try
                {
                    var contractId = room.CurrentContractId;
                    var deletedDraftContract = false;

                    property.ReleaseRoom(room.Id);
                    releasedRooms++;

                    if (contractId.HasValue)
                    {
                        var contract = await context.Contracts
                            .FirstOrDefaultAsync(c => c.Id == contractId.Value, cancellationToken);

                        if (contract != null && contract.Status == ContractStatus.Draft)
                        {
                            var payments = await context.Payments
                                .Where(p => p.ContractId == contract.Id)
                                .ToListAsync(cancellationToken);

                            if (payments.Count > 0)
                            {
                                context.Payments.RemoveRange(payments);
                            }

                            var documents = await context.Documents
                                .Where(d => d.ContractId == contract.Id)
                                .ToListAsync(cancellationToken);

                            if (documents.Count > 0)
                            {
                                context.Documents.RemoveRange(documents);
                            }

                            context.Contracts.Remove(contract);
                            deletedDraftContracts++;
                            deletedDraftContract = true;
                        }
                    }

                    _logger.LogInformation(
                        "⏳ Room OnHold expired => Released room {RoomId} for property {PropertyId} (ContractId: {ContractId}, DeletedDraftContract: {DeletedDraftContract})",
                        room.Id,
                        property.Id,
                        contractId,
                        deletedDraftContract);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Erreur lors du traitement expiration OnHold pour Room {RoomId} / Property {PropertyId}",
                        room.Id,
                        property.Id);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "✅ Expiration OnHold terminée: {ReleasedRooms} chambre(s) libérée(s), {DeletedDraftContracts} contrat(s) Draft supprimé(s)",
            releasedRooms,
            deletedDraftContracts);
    }

    private async Task UpdatePropertyStatusForActivatedContractAsync(
        LocaGuestDbContext context,
        Contract contract,
        CancellationToken cancellationToken)
    {
        var propertyQuery = context.Properties.AsQueryable();
        if (contract.RoomId.HasValue)
        {
            propertyQuery = propertyQuery.Include(p => p.Rooms);
        }

        var property = await propertyQuery
            .FirstOrDefaultAsync(p => p.Id == contract.PropertyId, cancellationToken);

        if (property == null)
        {
            return;
        }

        if (IsColocation(property.UsageType))
        {
            // Colocation: baser l'occupation sur l'état des chambres (UpdateOccupancyStatusFromRooms)
            context.Entry(property).Collection(p => p.Rooms).Load();

            if (contract.RoomId.HasValue)
            {
                property.OccupyRoom(contract.RoomId.Value, contract.Id);
            }
            else if (property.UsageType == PropertyUsageType.ColocationSolidaire)
            {
                // Colocation solidaire: 1 contrat => toutes les chambres occupées
                property.OccupyAllRooms(contract.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Contract {ContractCode} has no RoomId for colocation property {PropertyCode} (UsageType: {UsageType})",
                    contract.Code,
                    property.Code,
                    property.UsageType);
                return;
            }
        }
        else
        {
            // Location complète / colocation solidaire / Airbnb
            property.SetStatus(PropertyStatus.Active);
        }

        _logger.LogInformation(
            "✅ Bien {PropertyCode} mis à jour → {Status}",
            property.Code,
            property.Status);
    }

    private async Task UpdatePropertyStatusForExpiredContractAsync(
        LocaGuestDbContext context,
        Contract contract,
        CancellationToken cancellationToken)
    {
        var propertyQuery = context.Properties.AsQueryable();
        if (contract.RoomId.HasValue)
        {
            propertyQuery = propertyQuery.Include(p => p.Rooms);
        }

        var property = await propertyQuery
            .FirstOrDefaultAsync(p => p.Id == contract.PropertyId, cancellationToken);

        if (property == null)
        {
            return;
        }

        if (IsColocation(property.UsageType))
        {
            // Colocation: libérer la chambre, le statut se recalcule via UpdateOccupancyStatusFromRooms
            context.Entry(property).Collection(p => p.Rooms).Load();

            if (contract.RoomId.HasValue)
            {
                property.ReleaseRoom(contract.RoomId.Value);
            }
            else if (property.UsageType == PropertyUsageType.ColocationSolidaire)
            {
                // Colocation solidaire: libérer toutes les chambres
                property.ReleaseAllRooms();
            }
            else
            {
                _logger.LogWarning(
                    "Contract {ContractCode} has no RoomId for colocation property {PropertyCode} (UsageType: {UsageType})",
                    contract.Code,
                    property.Code,
                    property.UsageType);
                return;
            }
        }
        else
        {
            var hasOtherActiveContracts = await context.Contracts
                .AnyAsync(c =>
                        c.PropertyId == property.Id &&
                        c.Id != contract.Id &&
                        c.Status == ContractStatus.Active,
                    cancellationToken);

            if (hasOtherActiveContracts)
            {
                property.SetStatus(PropertyStatus.Active);
            }
            else
            {
                var hasOtherSignedContracts = await context.Contracts
                    .AnyAsync(c =>
                            c.PropertyId == property.Id &&
                            c.Id != contract.Id &&
                            c.Status == ContractStatus.Signed,
                        cancellationToken);

                property.SetStatus(hasOtherSignedContracts ? PropertyStatus.Reserved : PropertyStatus.Vacant);
            }
        }

        _logger.LogInformation(
            "✅ Bien {PropertyCode} mis à jour → {Status}",
            property.Code,
            property.Status);
    }
}
