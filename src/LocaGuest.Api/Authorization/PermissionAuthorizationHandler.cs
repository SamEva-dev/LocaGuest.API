using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace LocaGuest.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var permissions = GetPermissions(context.User);
        if (permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static HashSet<string> GetPermissions(ClaimsPrincipal user)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.FindAll("permissions").Concat(user.FindAll("permission")))
        {
            if (string.IsNullOrWhiteSpace(claim.Value)) continue;

            // Some JWT libraries encode array claims as a JSON array string: ["a","b"].
            // Others emit multiple claims with the same type. We support both.
            var value = claim.Value.Trim();
            if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
            {
                try
                {
                    var array = JsonSerializer.Deserialize<string[]>(value);
                    if (array is null) continue;
                    foreach (var p in array)
                    {
                        if (!string.IsNullOrWhiteSpace(p)) result.Add(p.Trim());
                    }
                    continue;
                }
                catch
                {
                    // fall through to delimiter splitting
                }
            }

            foreach (var p in value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(p.Trim());
            }
        }

        return result;
    }
}
