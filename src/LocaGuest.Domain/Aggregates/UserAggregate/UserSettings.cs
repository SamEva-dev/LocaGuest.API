using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.UserAggregate;

public class UserSettings : AuditableEntity
{
    public Guid UserId { get; private set; }
    
    // Profile
    public string? PhotoUrl { get; private set; }
    
    // Notifications
    public bool EmailAlerts { get; private set; } = true;
    public bool SmsAlerts { get; private set; } = false;
    public bool NewReservations { get; private set; } = true;
    public bool PaymentReminders { get; private set; } = true;
    public bool MonthlyReports { get; private set; } = true;
    
    // Preferences
    public bool DarkMode { get; private set; } = false;
    public string Language { get; private set; } = "fr";
    public string Timezone { get; private set; } = "Europe/Paris";
    public string DateFormat { get; private set; } = "DD/MM/YYYY";
    public string Currency { get; private set; } = "EUR";
    
    // Interface
    public bool SidebarNavigation { get; private set; } = true;
    public bool HeaderNavigation { get; private set; } = false;
    public DateTime UpdatedAt { get; private set; }

    private UserSettings() { }

    public static UserSettings Create(Guid userId)
    {
        return new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotifications(
        bool emailAlerts,
        bool smsAlerts,
        bool newReservations,
        bool paymentReminders,
        bool monthlyReports)
    {
        EmailAlerts = emailAlerts;
        SmsAlerts = smsAlerts;
        NewReservations = newReservations;
        PaymentReminders = paymentReminders;
        MonthlyReports = monthlyReports;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(
        bool darkMode,
        string language,
        string timezone,
        string dateFormat,
        string currency)
    {
        DarkMode = darkMode;
        Language = language;
        Timezone = timezone;
        DateFormat = dateFormat;
        Currency = currency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInterface(
        bool sidebarNavigation,
        bool headerNavigation)
    {
        SidebarNavigation = sidebarNavigation;
        HeaderNavigation = headerNavigation;
        UpdatedAt = DateTime.UtcNow;
    }
}
