using LocaGuest.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LocaGuest.Infrastructure.Services;

/// <summary>
/// Service to extract tenant context from JWT claims
/// </summary>
public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id");
        
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        
        return null;
    }

    public string? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");
        
        return userIdClaim?.Value;
    }

    public bool IsSuperAdmin()
    {
        var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
        
        return roles.Contains("SuperAdmin");
    }
}
