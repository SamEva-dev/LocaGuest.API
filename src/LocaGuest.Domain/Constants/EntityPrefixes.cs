namespace LocaGuest.Domain.Constants;

/// <summary>
/// Standard prefixes for entity code generation
/// Format: {TenantCode}-{Prefix}{Number:0000}
/// </summary>
public static class EntityPrefixes
{
    /// <summary>
    /// Appartement - APP
    /// Example: T0001-APP0001
    /// </summary>
    public const string Property = "APP";

    /// <summary>
    /// Locataire/Occupant - L
    /// Example: T0001-L0001
    /// </summary>
    public const string Tenant = "L";
    
    /// <summary>
    /// Alias for Tenant - Occupant
    /// </summary>
    public const string Occupant = "L";

    /// <summary>
    /// Maison - M
    /// Example: T0001-M0001
    /// </summary>
    public const string House = "M";

    /// <summary>
    /// Contrat - CTR
    /// Example: T0001-CTR0001
    /// </summary>
    public const string Contract = "CTR";

    /// <summary>
    /// Document - DOC
    /// </summary>
    public const string Document = "DOC";

    /// <summary>
    /// Paiement - PAY
    /// Example: T0001-PAY0001
    /// </summary>
    public const string Payment = "PAY";

    /// <summary>
    /// Scénario - SCN
    /// Example: T0001-SCN0001
    /// </summary>
    public const string Scenario = "SCN";

    /// <summary>
    /// Facture - INV
    /// Example: T0001-INV0001
    /// </summary>
    public const string Invoice = "INV";

    /// <summary>
    /// Get all supported prefixes
    /// </summary>
    public static readonly string[] All = {Property, Tenant, Occupant, House, Contract, Payment, Invoice};

    /// <summary>
    /// Get description for a prefix
    /// </summary>
    public static string GetDescription(string prefix)
    {
        return prefix switch
        {
            Property => "Appartement",
            Tenant => "Locataire",
            House => "Maison",
            Contract => "Contrat",
            Payment => "Paiement",
            Invoice => "Facture",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Validate if prefix is supported
    /// </summary>
    public static bool IsValid(string prefix)
    {
        return Array.Exists(All, p => p == prefix);
    }
}
