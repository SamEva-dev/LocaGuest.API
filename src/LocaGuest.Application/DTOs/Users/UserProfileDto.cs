namespace LocaGuest.Application.DTOs.Users;

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateUserProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Bio { get; set; }
}

public class UserPreferencesDto
{
    public bool DarkMode { get; set; }
    public string Language { get; set; } = "fr";
    public string Timezone { get; set; } = "Europe/Paris";
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string Currency { get; set; } = "EUR";
    public bool SidebarNavigation { get; set; }
    public bool HeaderNavigation { get; set; }
}

public class NotificationSettingsDto
{
    public bool PaymentReceived { get; set; }
    public bool PaymentOverdue { get; set; }
    public bool PaymentReminder { get; set; }
    public bool ContractSigned { get; set; }
    public bool ContractExpiring { get; set; }
    public bool ContractRenewal { get; set; }
    public bool NewTenantRequest { get; set; }
    public bool TenantCheckout { get; set; }
    public bool MaintenanceRequest { get; set; }
    public bool MaintenanceCompleted { get; set; }
    public bool SystemUpdates { get; set; }
    public bool MarketingEmails { get; set; }
}
