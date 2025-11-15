using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.SubscriptionAggregate;

public class Plan : AuditableEntity
{
    public string Code { get; private set; } = string.Empty; // free, pro, business, enterprise
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal MonthlyPrice { get; private set; }
    public decimal AnnualPrice { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    
    // Limites et quotas
    public int MaxScenarios { get; private set; }
    public int MaxExportsPerMonth { get; private set; }
    public int MaxVersionsPerScenario { get; private set; }
    public int MaxShares { get; private set; }
    public int MaxAiSuggestionsPerMonth { get; private set; }
    public int MaxWorkspaces { get; private set; }
    public int MaxTeamMembers { get; private set; }
    
    // Features flags
    public bool HasUnlimitedExports { get; private set; }
    public bool HasUnlimitedVersioning { get; private set; }
    public bool HasUnlimitedAi { get; private set; }
    public bool HasPrivateTemplates { get; private set; }
    public bool HasTeamTemplates { get; private set; }
    public bool HasAdvancedComparison { get; private set; }
    public bool HasApiAccess { get; private set; }
    public bool HasApiReadWrite { get; private set; }
    public bool HasEmailNotifications { get; private set; }
    public bool HasSlackIntegration { get; private set; }
    public bool HasWebhooks { get; private set; }
    public bool HasSso { get; private set; }
    public bool HasPrioritySupport { get; private set; }
    public bool HasDedicatedSupport { get; private set; }
    
    // Stripe IDs
    public string? StripeMonthlyPriceId { get; private set; }
    public string? StripeAnnualPriceId { get; private set; }
    
    private Plan() { }
    
    public static Plan Create(
        string code,
        string name,
        string description,
        decimal monthlyPrice,
        decimal annualPrice,
        int sortOrder = 0)
    {
        return new Plan
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            MonthlyPrice = monthlyPrice,
            AnnualPrice = annualPrice,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateLimits(
        int maxScenarios,
        int maxExportsPerMonth,
        int maxVersionsPerScenario,
        int maxShares,
        int maxAiSuggestionsPerMonth,
        int maxWorkspaces,
        int maxTeamMembers)
    {
        MaxScenarios = maxScenarios;
        MaxExportsPerMonth = maxExportsPerMonth;
        MaxVersionsPerScenario = maxVersionsPerScenario;
        MaxShares = maxShares;
        MaxAiSuggestionsPerMonth = maxAiSuggestionsPerMonth;
        MaxWorkspaces = maxWorkspaces;
        MaxTeamMembers = maxTeamMembers;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateFeatures(
        bool unlimitedExports,
        bool unlimitedVersioning,
        bool unlimitedAi,
        bool privateTemplates,
        bool teamTemplates,
        bool advancedComparison,
        bool apiAccess,
        bool apiReadWrite,
        bool emailNotifications,
        bool slackIntegration,
        bool webhooks,
        bool sso,
        bool prioritySupport,
        bool dedicatedSupport)
    {
        HasUnlimitedExports = unlimitedExports;
        HasUnlimitedVersioning = unlimitedVersioning;
        HasUnlimitedAi = unlimitedAi;
        HasPrivateTemplates = privateTemplates;
        HasTeamTemplates = teamTemplates;
        HasAdvancedComparison = advancedComparison;
        HasApiAccess = apiAccess;
        HasApiReadWrite = apiReadWrite;
        HasEmailNotifications = emailNotifications;
        HasSlackIntegration = slackIntegration;
        HasWebhooks = webhooks;
        HasSso = sso;
        HasPrioritySupport = prioritySupport;
        HasDedicatedSupport = dedicatedSupport;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void SetStripePriceIds(string? monthlyPriceId, string? annualPriceId)
    {
        StripeMonthlyPriceId = monthlyPriceId;
        StripeAnnualPriceId = annualPriceId;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }
}
