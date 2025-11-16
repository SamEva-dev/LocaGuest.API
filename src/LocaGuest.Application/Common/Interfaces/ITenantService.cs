namespace LocaGuest.Application.Common.Interfaces;

/// <summary>
/// Service for accessing tenant context from JWT claims
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant ID from JWT claims
    /// </summary>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Gets the current user ID from JWT claims
    /// </summary>
    string? GetCurrentUserId();

    /// <summary>
    /// Checks if the current user is a SuperAdmin
    /// </summary>
    bool IsSuperAdmin();
}
