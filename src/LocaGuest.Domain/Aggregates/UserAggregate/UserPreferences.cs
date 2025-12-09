using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.UserAggregate;

/// <summary>
/// Préférences utilisateur pour l'interface et l'expérience
/// </summary>
public class UserPreferences : AuditableEntity
{
    public string UserId { get; private set; } = string.Empty;
    
    // Interface
    public bool DarkMode { get; private set; }
    public string Language { get; private set; } = "fr"; // fr, en
    public string Timezone { get; private set; } = "Europe/Paris";
    public string DateFormat { get; private set; } = "DD/MM/YYYY";
    public string Currency { get; private set; } = "EUR";
    
    // Navigation
    public bool SidebarNavigation { get; private set; } = true;
    public bool HeaderNavigation { get; private set; } = false;
    
    private UserPreferences() { } // EF

    public static UserPreferences CreateDefault(string userId)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DarkMode = false,
            Language = "fr",
            Timezone = "Europe/Paris",
            DateFormat = "DD/MM/YYYY",
            Currency = "EUR",
            SidebarNavigation = true,
            HeaderNavigation = false
        };
    }

    public void Update(
        bool darkMode,
        string language,
        string timezone,
        string dateFormat,
        string currency,
        bool sidebarNavigation,
        bool headerNavigation)
    {
        DarkMode = darkMode;
        Language = language;
        Timezone = timezone;
        DateFormat = dateFormat;
        Currency = currency;
        SidebarNavigation = sidebarNavigation;
        HeaderNavigation = headerNavigation;
    }
}
