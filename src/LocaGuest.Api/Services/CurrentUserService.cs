using LocaGuest.Application.Services;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Infrastructure.Persistence;
using System.Security.Claims;

namespace LocaGuest.Api.Services;

public class CurrentUserService : ICurrentUserService, ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }

    public string? UserEmail =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;
            
            // Try X-Forwarded-For first (if behind proxy)
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }
            
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();

    public Guid? TenantId
    {
        get
        {
            // Return null if no HTTP context (e.g., during seeding, background jobs)
            if (_httpContextAccessor.HttpContext == null)
                return null;
            
            // Return null if user is not authenticated
            if (!IsAuthenticated)
                return null;
            
            var tenantIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id")
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId");
            
            // Return null if claim not found (will be caught by services that require it)
            if (string.IsNullOrEmpty(tenantIdStr))
                return null;
            
            return Guid.TryParse(tenantIdStr, out var tenantId) ? tenantId : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
        && UserId.HasValue;
}
