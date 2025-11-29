using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Services.BackgroundJobs;

/// <summary>
/// Service d'arrière-plan pour activer automatiquement les contrats signés
/// dont la date de début est atteinte
/// Exécution: Toutes les heures
/// </summary>
public class ContractActivationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContractActivationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Toutes les heures

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

        var today = DateTime.UtcNow.Date;

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

                // Charger le bien associé
                var property = await context.Properties.FindAsync(contract.PropertyId);
                if (property != null)
                {
                    if (property.UsageType == PropertyUsageType.ColocationIndividual ||
                        property.UsageType == PropertyUsageType.Colocation)
                    {
                        // Colocation individuelle: incrémenter occupiedRooms
                        property.IncrementOccupiedRooms();
                        
                        if (property.OccupiedRooms >= (property.TotalRooms ?? 0))
                        {
                            property.SetStatus(PropertyStatus.Occupied);
                        }
                        else
                        {
                            property.SetStatus(PropertyStatus.PartiallyOccupied);
                        }
                    }
                    else
                    {
                        // Location complète ou colocation solidaire
                        property.SetStatus(PropertyStatus.Occupied);
                    }
                    
                    _logger.LogInformation(
                        "✅ Bien {PropertyCode} mis à jour → {Status}",
                        property.Code,
                        property.Status);
                }

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

        var today = DateTime.UtcNow.Date;

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

                // Charger le bien associé
                var property = await context.Properties.FindAsync(contract.PropertyId);
                if (property != null)
                {
                    if (property.UsageType == PropertyUsageType.ColocationIndividual ||
                        property.UsageType == PropertyUsageType.Colocation)
                    {
                        // Colocation individuelle: décrémenter occupiedRooms
                        property.DecrementOccupiedRooms();
                        
                        if (property.OccupiedRooms == 0)
                        {
                            property.SetStatus(PropertyStatus.Vacant);
                        }
                        else
                        {
                            property.SetStatus(PropertyStatus.PartiallyOccupied);
                        }
                    }
                    else
                    {
                        // Vérifier s'il reste d'autres contrats actifs
                        var hasOtherActiveContracts = await context.Contracts
                            .AnyAsync(c => 
                                c.PropertyId == property.Id &&
                                c.Id != contract.Id &&
                                c.Status == ContractStatus.Active,
                                cancellationToken);

                        if (!hasOtherActiveContracts)
                        {
                            property.SetStatus(PropertyStatus.Vacant);
                        }
                    }
                    
                    _logger.LogInformation(
                        "✅ Bien {PropertyCode} mis à jour → {Status}",
                        property.Code,
                        property.Status);
                }

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
}
