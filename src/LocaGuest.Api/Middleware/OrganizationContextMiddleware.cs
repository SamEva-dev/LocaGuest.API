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

        Guid? organizationId = null;

        if (isAuthenticated)
        {
            var val =
                user.FindFirstValue("organization_id")
                ?? user.FindFirstValue("organizationId");

            if (!Guid.TryParse(val, out var parsed))
            {
                writer.Set(null, isAuthenticated: true);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid organization_id claim." });
                return;
            }

            organizationId = parsed;
        }

        writer.Set(organizationId, isAuthenticated);

        await _next(context);
    }
}
