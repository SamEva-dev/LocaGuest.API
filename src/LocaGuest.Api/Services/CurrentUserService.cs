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

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? "anonymous";

    public string TenantId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId")
        ?? throw new UnauthorizedAccessException("TenantId not found in JWT token");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
        && !string.IsNullOrEmpty(UserId)
        && UserId != "anonymous";
}
