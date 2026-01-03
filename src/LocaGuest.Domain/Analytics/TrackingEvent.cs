namespace LocaGuest.Domain.Analytics;

/// <summary>
/// Tracking event for analytics and user behavior tracking
/// Separate from Audit (security) - focused on product usage and UX
/// </summary>
public class TrackingEvent
{
    /// <summary>
    /// Identifiant unique de l'événement.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Tenant SaaS (organisation/compte) - requis pour l'isolation multi-tenant.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Identifiant de l'utilisateur à l'origine de l'événement.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type d'événement (ex: PAGE_VIEW, API_REQUEST...).
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Nom de la page (si applicable).
    /// </summary>
    public string? PageName { get; private set; }

    /// <summary>
    /// URL (si applicable).
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// User-Agent du client.
    /// </summary>
    public string UserAgent { get; private set; } = string.Empty;

    /// <summary>
    /// Adresse IP anonymisée (RGPD).
    /// </summary>
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp UTC de l'événement.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Données additionnelles (JSON) liées à l'événement.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Identifiant de session (si applicable).
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// Identifiant de corrélation (si applicable).
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Durée de l'opération en millisecondes (si applicable).
    /// </summary>
    public int? DurationMs { get; private set; }

    /// <summary>
    /// Code de statut HTTP (si applicable).
    /// </summary>
    public int? HttpStatusCode { get; private set; }
    
    private TrackingEvent() { }
    
    public static TrackingEvent Create(
        Guid organizationId,
        Guid userId,
        string eventType,
        string ipAddress,
        string userAgent,
        string? pageName = null,
        string? url = null,
        string? metadata = null,
        string? sessionId = null,
        string? correlationId = null)
    {
        return new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            EventType = eventType,
            PageName = pageName,
            Url = url,
            IpAddress = AnonymizeIp(ipAddress),
            UserAgent = userAgent,
            Metadata = metadata,
            SessionId = sessionId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void SetPerformanceMetrics(int durationMs, int? statusCode = null)
    {
        DurationMs = durationMs;
        HttpStatusCode = statusCode;
    }
    
    /// <summary>
    /// Anonymize IP address for GDPR compliance
    /// Example: 192.168.1.100 -> 192.168.1.0
    /// </summary>
    private static string AnonymizeIp(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "0.0.0.0";
        
        var parts = ipAddress.Split('.');
        if (parts.Length == 4)
        {
            // IPv4: anonymize last octet
            return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        }
        
        // IPv6: anonymize last 80 bits (keep first 48 bits)
        if (ipAddress.Contains(':'))
        {
            var ipv6Parts = ipAddress.Split(':');
            if (ipv6Parts.Length >= 3)
            {
                return $"{ipv6Parts[0]}:{ipv6Parts[1]}:{ipv6Parts[2]}::0";
            }
        }
        
        return "anonymized";
    }
}

/// <summary>
/// Standard event types for tracking
/// </summary>
public static class TrackingEventTypes
{
    // Page views
    public const string PageView = "PAGE_VIEW";
    public const string PageExit = "PAGE_EXIT";
    
    // API requests
    public const string ApiRequest = "API_REQUEST";
    
    // User actions
    public const string ButtonClick = "BUTTON_CLICK";
    public const string FormSubmit = "FORM_SUBMIT";
    public const string DownloadFile = "DOWNLOAD_FILE";
    
    // Business actions
    public const string PropertyCreated = "PROPERTY_CREATED";
    public const string ContractCreated = "CONTRACT_CREATED";
    public const string TenantCreated = "TENANT_CREATED";
    public const string PaymentRecorded = "PAYMENT_RECORDED";
    public const string DocumentGenerated = "DOCUMENT_GENERATED";
    public const string ReminderSent = "REMINDER_SENT";
    
    // Feature usage
    public const string FeatureUsed = "FEATURE_USED";
    public const string SearchPerformed = "SEARCH_PERFORMED";
    public const string FilterApplied = "FILTER_APPLIED";
    public const string ExportTriggered = "EXPORT_TRIGGERED";
    
    // Navigation
    public const string TabChanged = "TAB_CHANGED";
    public const string ModalOpened = "MODAL_OPENED";
    public const string ModalClosed = "MODAL_CLOSED";
    
    // Authentication
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string SessionStart = "SESSION_START";
    public const string SessionEnd = "SESSION_END";
    
    // Subscription
    public const string UpgradeClicked = "UPGRADE_CLICKED";
    public const string PricingPageViewed = "PRICING_PAGE_VIEWED";
    public const string CheckoutStarted = "CHECKOUT_STARTED";
    
    // Errors
    public const string ErrorOccurred = "ERROR_OCCURRED";
    public const string ApiError = "API_ERROR";
}
