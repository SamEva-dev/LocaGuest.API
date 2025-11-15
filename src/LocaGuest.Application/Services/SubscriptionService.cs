using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ILocaGuestDbContext _context;
    
    public SubscriptionService(ILocaGuestDbContext context)
    {
        _context = context;
    }
    
    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && (s.Status == "active" || s.Status == "trialing"))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<Plan> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);
        
        if (subscription != null)
        {
            return subscription.Plan;
        }
        
        // Retourner le plan Free par défaut
        var freePlan = await _context.Plans
            .FirstOrDefaultAsync(p => p.Code == "free", cancellationToken);
            
        if (freePlan == null)
        {
            throw new InvalidOperationException("Free plan not found in database");
        }
        
        return freePlan;
    }
    
    public async Task<bool> CanAccessFeatureAsync(Guid userId, string featureName, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(userId, cancellationToken);
        
        return featureName switch
        {
            "unlimited_exports" => plan.HasUnlimitedExports,
            "unlimited_versioning" => plan.HasUnlimitedVersioning,
            "unlimited_ai" => plan.HasUnlimitedAi,
            "private_templates" => plan.HasPrivateTemplates,
            "team_templates" => plan.HasTeamTemplates,
            "advanced_comparison" => plan.HasAdvancedComparison,
            "api_access" => plan.HasApiAccess,
            "api_read_write" => plan.HasApiReadWrite,
            "email_notifications" => plan.HasEmailNotifications,
            "slack_integration" => plan.HasSlackIntegration,
            "webhooks" => plan.HasWebhooks,
            "sso" => plan.HasSso,
            "priority_support" => plan.HasPrioritySupport,
            "dedicated_support" => plan.HasDedicatedSupport,
            _ => false
        };
    }
    
    public async Task<bool> CheckQuotaAsync(Guid userId, string dimension, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(userId, cancellationToken);
        var currentUsage = await GetUsageAsync(userId, dimension, cancellationToken);
        
        var limit = dimension switch
        {
            "scenarios" => plan.MaxScenarios,
            "exports" => plan.HasUnlimitedExports ? int.MaxValue : plan.MaxExportsPerMonth,
            "ai_suggestions" => plan.HasUnlimitedAi ? int.MaxValue : plan.MaxAiSuggestionsPerMonth,
            "shares" => plan.MaxShares,
            "workspaces" => plan.MaxWorkspaces,
            _ => 0
        };
        
        return currentUsage < limit;
    }
    
    public async Task<int> GetUsageAsync(Guid userId, string dimension, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        // Pour les quotas mensuels, utiliser les agrégats
        var aggregate = await _context.UsageAggregates
            .Where(u => u.UserId == userId 
                     && u.Dimension == dimension 
                     && u.PeriodYear == now.Year 
                     && u.PeriodMonth == now.Month)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (aggregate != null)
        {
            return aggregate.TotalValue;
        }
        
        // Pour les scénarios actifs, compter directement
        if (dimension == "scenarios")
        {
            return await _context.RentabilityScenarios
                .Where(s => s.UserId == userId)
                .CountAsync(cancellationToken);
        }
        
        return 0;
    }
    
    public async Task RecordUsageAsync(Guid userId, string dimension, int value = 1, string? metadata = null, CancellationToken cancellationToken = default)
    {
        var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);
        
        if (subscription == null)
        {
            // Pas d'abonnement actif, on ne peut pas enregistrer l'usage
            return;
        }
        
        // Enregistrer l'événement
        subscription.RecordUsage(dimension, value, metadata);
        
        // Mettre à jour l'agrégat mensuel
        var now = DateTime.UtcNow;
        var aggregate = await _context.UsageAggregates
            .Where(u => u.UserId == userId 
                     && u.SubscriptionId == subscription.Id
                     && u.Dimension == dimension 
                     && u.PeriodYear == now.Year 
                     && u.PeriodMonth == now.Month)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (aggregate == null)
        {
            aggregate = UsageAggregate.Create(userId, subscription.Id, dimension, now.Year, now.Month, value);
            _context.UsageAggregates.Add(aggregate);
        }
        else
        {
            aggregate.Increment(value);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
