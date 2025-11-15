using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.SubscriptionAggregate;

public class Subscription : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public Plan Plan { get; private set; } = null!;
    
    public string Status { get; private set; } = string.Empty; // trialing, active, past_due, canceled, unpaid
    public bool IsAnnual { get; private set; }
    
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public DateTime? CancelAt { get; private set; } // Scheduled cancellation
    
    // Stripe
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public string? StripeLatestInvoiceId { get; private set; }
    
    // Usage tracking
    private readonly List<UsageEvent> _usageEvents = new();
    public IReadOnlyCollection<UsageEvent> UsageEvents => _usageEvents.AsReadOnly();
    
    private Subscription() { }
    
    public static Subscription Create(
        Guid userId,
        Guid planId,
        bool isAnnual = false,
        int trialDays = 14)
    {
        var now = DateTime.UtcNow;
        var trialEnd = trialDays > 0 ? now.AddDays(trialDays) : (DateTime?)null;
        
        return new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Status = trialDays > 0 ? "trialing" : "active",
            IsAnnual = isAnnual,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = isAnnual ? now.AddYears(1) : now.AddMonths(1),
            TrialEndsAt = trialEnd,
            CreatedAt = now
        };
    }
    
    public void UpdateStatus(string status)
    {
        Status = status;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateStatus(string status, DateTime periodStart, DateTime periodEnd)
    {
        Status = status;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdatePeriod(DateTime periodStart, DateTime periodEnd)
    {
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void SetStripeInfo(string customerId, string subscriptionId)
    {
        StripeCustomerId = customerId;
        StripeSubscriptionId = subscriptionId;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateLatestInvoice(string invoiceId)
    {
        StripeLatestInvoiceId = invoiceId;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Cancel(bool immediate = false)
    {
        CanceledAt = DateTime.UtcNow;
        if (immediate)
        {
            Status = "canceled";
            CancelAt = DateTime.UtcNow;
        }
        else
        {
            // Cancel at period end
            CancelAt = CurrentPeriodEnd;
        }
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void Reactivate()
    {
        CanceledAt = null;
        CancelAt = null;
        Status = "active";
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void ChangePlan(Guid newPlanId, bool isAnnual)
    {
        PlanId = newPlanId;
        IsAnnual = isAnnual;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public bool IsActive() => Status is "trialing" or "active";
    
    public bool IsInTrial() => Status == "trialing" && TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
    
    public int DaysUntilRenewal() => (CurrentPeriodEnd - DateTime.UtcNow).Days;
    
    public void RecordUsage(string dimension, int value, string? metadata = null)
    {
        var usageEvent = UsageEvent.Create(Id, UserId, dimension, value, metadata);
        _usageEvents.Add(usageEvent);
    }
}
