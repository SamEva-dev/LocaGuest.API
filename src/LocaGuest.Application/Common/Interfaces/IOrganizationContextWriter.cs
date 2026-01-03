namespace LocaGuest.Application.Common.Interfaces;

/// <summary>
/// Ecriture du contexte d'organisation (réservée au pipeline HTTP / jobs internes).
/// </summary>
public interface IOrganizationContextWriter
{
    /// <summary>
    /// Configure le contexte d'organisation courant.
    /// </summary>
    /// <param name="organizationId">Id d'organisation à appliquer (null si non applicable).</param>
    /// <param name="isAuthenticated">Indique si une identité est authentifiée.</param>
    /// <param name="isSystemContext">Indique un contexte système interne.</param>
    /// <param name="canBypassOrganizationFilter">Autorise explicitement le bypass du filtre multi-organisation.</param>
    void Set(
        Guid? organizationId,
        bool isAuthenticated,
        bool isSystemContext = false,
        bool canBypassOrganizationFilter = false);
}
