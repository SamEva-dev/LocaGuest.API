namespace LocaGuest.Application.DTOs.Settings;

public class UserProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
}

public class NotificationSettingsDto
{
    public bool EmailAlerts { get; set; }
    public bool SmsAlerts { get; set; }
    public bool NewReservations { get; set; }
    public bool PaymentReminders { get; set; }
    public bool MonthlyReports { get; set; }
}

public class PreferencesDto
{
    public bool DarkMode { get; set; }
    public string Language { get; set; } = "fr";
    public string Timezone { get; set; } = "Europe/Paris";
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string Currency { get; set; } = "EUR";
}

public class InterfaceSettingsDto
{
    public bool SidebarNavigation { get; set; } = true;
    public bool HeaderNavigation { get; set; } = false;
}

public class UserSettingsDto
{
    public UserProfileDto Profile { get; set; } = new();
    public NotificationSettingsDto Notifications { get; set; } = new();
    public PreferencesDto Preferences { get; set; } = new();
    public InterfaceSettingsDto Interface { get; set; } = new();
}
