using System.Security.Claims;
using LocaGuest.Application.Common.Interfaces;

namespace LocaGuest.Api.Middleware;

public sealed class OrganizationContextMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IOrganizationContextWriter writer)
    {
        var user = context.User;
        var isAuthenticated = user?.Identity?.IsAuthenticated == true;

        if (!isAuthenticated)
        {
            writer.Set(null, isAuthenticated: false);
            await _next(context);
            return;
        }

        // Exception contrôlée : provisioning token (M2M)
        var scope = user?.FindFirst("scope")?.Value ?? string.Empty;
        var isProvisioning = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("locaguest.provisioning");

        if (isProvisioning)
        {
            // Pas d'orgId au début du provisioning (normal)
            writer.Set(null, isAuthenticated: true, isSystemContext: true, canBypassOrganizationFilter: true);
            await _next(context);
            return;
        }

        // Mode standard : org obligatoire
        var val =
            user?.FindFirstValue("organization_id")
            ?? user?.FindFirstValue("organizationId");

        if (!Guid.TryParse(val, out var parsed))
        {
            writer.Set(null, isAuthenticated: true);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid organization_id claim." });
            return;
        }

        writer.Set(parsed, isAuthenticated: true);

        await _next(context);
    }
}
