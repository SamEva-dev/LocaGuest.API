using LocaGuest.Application.Common.Interfaces;

namespace LocaGuest.Api.Services;

public sealed class OrganizationContextAccessor : IOrganizationContext, IOrganizationContextWriter
{
    /// <summary>
    /// Id de l'organisation (tenant SaaS) courant.
    /// </summary>
    public Guid? OrganizationId { get; private set; }

    /// <summary>
    /// Indique si une identité est authentifiée (contexte HTTP ou service).
    /// </summary>
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Indique un contexte "system" réservé aux scénarios internes explicitement autorisés
    /// (jobs, migrations, provisioning).
    /// </summary>
    public bool IsSystemContext { get; private set; }

    /// <summary>
    /// Bypass du filtre multi-organisation : uniquement si IsSystemContext est vrai.
    /// Doit rester FALSE par défaut.
    /// </summary>
    public bool CanBypassOrganizationFilter { get; private set; }

    /// <summary>
    /// Configure le contexte d'organisation courant.
    /// </summary>
    public void Set(
        Guid? organizationId,
        bool isAuthenticated,
        bool isSystemContext = false,
        bool canBypassOrganizationFilter = false)
    {
        OrganizationId = organizationId;
        IsAuthenticated = isAuthenticated;
        IsSystemContext = isSystemContext;
        CanBypassOrganizationFilter = canBypassOrganizationFilter;
    }
}
