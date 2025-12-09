using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.UserAggregate;

/// <summary>
/// Paramètres de notifications pour différents événements
/// </summary>
public class NotificationSettings : AuditableEntity
{
    public string UserId { get; private set; } = string.Empty;
    
    // Paiements
    public bool PaymentReceived { get; private set; } = true;
    public bool PaymentOverdue { get; private set; } = true;
    public bool PaymentReminder { get; private set; } = true;
    
    // Contrats
    public bool ContractSigned { get; private set; } = true;
    public bool ContractExpiring { get; private set; } = true;
    public bool ContractRenewal { get; private set; } = true;
    
    // Locataires
    public bool NewTenantRequest { get; private set; } = true;
    public bool TenantCheckout { get; private set; } = true;
    
    // Maintenance
    public bool MaintenanceRequest { get; private set; } = true;
    public bool MaintenanceCompleted { get; private set; } = false;
    
    // Système
    public bool SystemUpdates { get; private set; } = true;
    public bool MarketingEmails { get; private set; } = false;
    
    private NotificationSettings() { } // EF

    public static NotificationSettings CreateDefault(string userId)
    {
        return new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentReceived = true,
            PaymentOverdue = true,
            PaymentReminder = true,
            ContractSigned = true,
            ContractExpiring = true,
            ContractRenewal = true,
            NewTenantRequest = true,
            TenantCheckout = true,
            MaintenanceRequest = true,
            MaintenanceCompleted = false,
            SystemUpdates = true,
            MarketingEmails = false
        };
    }

    public void Update(Dictionary<string, bool> settings)
    {
        if (settings.TryGetValue("paymentReceived", out var paymentReceived))
            PaymentReceived = paymentReceived;
        if (settings.TryGetValue("paymentOverdue", out var paymentOverdue))
            PaymentOverdue = paymentOverdue;
        if (settings.TryGetValue("paymentReminder", out var paymentReminder))
            PaymentReminder = paymentReminder;
        if (settings.TryGetValue("contractSigned", out var contractSigned))
            ContractSigned = contractSigned;
        if (settings.TryGetValue("contractExpiring", out var contractExpiring))
            ContractExpiring = contractExpiring;
        if (settings.TryGetValue("contractRenewal", out var contractRenewal))
            ContractRenewal = contractRenewal;
        if (settings.TryGetValue("newTenantRequest", out var newTenantRequest))
            NewTenantRequest = newTenantRequest;
        if (settings.TryGetValue("tenantCheckout", out var tenantCheckout))
            TenantCheckout = tenantCheckout;
        if (settings.TryGetValue("maintenanceRequest", out var maintenanceRequest))
            MaintenanceRequest = maintenanceRequest;
        if (settings.TryGetValue("maintenanceCompleted", out var maintenanceCompleted))
            MaintenanceCompleted = maintenanceCompleted;
        if (settings.TryGetValue("systemUpdates", out var systemUpdates))
            SystemUpdates = systemUpdates;
        if (settings.TryGetValue("marketingEmails", out var marketingEmails))
            MarketingEmails = marketingEmails;
    }

    public void UpdateAll(
        bool paymentReceived,
        bool paymentOverdue,
        bool paymentReminder,
        bool contractSigned,
        bool contractExpiring,
        bool contractRenewal,
        bool newTenantRequest,
        bool tenantCheckout,
        bool maintenanceRequest,
        bool maintenanceCompleted,
        bool systemUpdates,
        bool marketingEmails)
    {
        PaymentReceived = paymentReceived;
        PaymentOverdue = paymentOverdue;
        PaymentReminder = paymentReminder;
        ContractSigned = contractSigned;
        ContractExpiring = contractExpiring;
        ContractRenewal = contractRenewal;
        NewTenantRequest = newTenantRequest;
        TenantCheckout = tenantCheckout;
        MaintenanceRequest = maintenanceRequest;
        MaintenanceCompleted = maintenanceCompleted;
        SystemUpdates = systemUpdates;
        MarketingEmails = marketingEmails;
    }
}
