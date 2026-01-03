namespace LocaGuest.Application.Common.Interfaces;

/// <summary>
/// Contexte d'isolation multi-tenant (tenant SaaS = organisation/compte).
/// </summary>
public interface IOrganizationContext
{
    /// <summary>
    /// Id de l'organisation (tenant SaaS) courant.
    /// </summary>
    Guid? OrganizationId { get; }

    /// <summary>
    /// Indique si une identité est authentifiée (contexte HTTP ou service).
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indique un contexte "system" réservé aux scénarios internes explicitement autorisés
    /// (jobs, migrations, provisioning).
    /// </summary>
    bool IsSystemContext { get; }

    /// <summary>
    /// Bypass du filtre multi-organisation : uniquement si <see cref="IsSystemContext"/> est vrai.
    /// Doit rester FALSE par défaut.
    /// </summary>
    bool CanBypassOrganizationFilter { get; }
}
