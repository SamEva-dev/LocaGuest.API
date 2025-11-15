namespace LocaGuest.Application.Common.Interfaces;

/// <summary>
/// Service pour extraire le contexte du tenant courant depuis le JWT
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Id du tenant courant (organisation/compte) extrait du token JWT
    /// </summary>
    Guid? TenantId { get; }
    
    /// <summary>
    /// Id de l'utilisateur courant extrait du token JWT
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Vérifie si l'utilisateur est authentifié et a un tenant
    /// </summary>
    bool IsAuthenticated { get; }
}
