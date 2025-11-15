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
            var tenantIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id")
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId");
            
            if (string.IsNullOrEmpty(tenantIdStr))
                throw new UnauthorizedAccessException("TenantId not found in JWT token");
            
            return Guid.TryParse(tenantIdStr, out var tenantId) ? tenantId : throw new UnauthorizedAccessException("Invalid TenantId format");
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
        && UserId.HasValue;
}
