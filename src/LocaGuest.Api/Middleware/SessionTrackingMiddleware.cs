using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using System.Security.Claims;

namespace LocaGuest.Api.Middleware;

public sealed class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public SessionTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IServiceScopeFactory serviceScopeFactory,
        ICurrentUserService currentUserService)
    {
        await _next(context);

        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return;

        var jti = context.User.FindFirstValue("jti");
        if (string.IsNullOrWhiteSpace(jti))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var cu = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

                var session = await uow.UserSessions.GetBySessionTokenAsync(jti);

                var device = string.Empty;
                var browser = string.Empty;
                var ip = cu.IpAddress ?? "unknown";

                if (session == null)
                {
                    var created = UserSession.Create(
                        userId: cu.UserId!.Value.ToString(),
                        sessionToken: jti,
                        deviceName: device,
                        browser: browser,
                        ipAddress: ip,
                        location: string.Empty);

                    await uow.UserSessions.AddAsync(created);
                }
                else
                {
                    session.UpdateActivity();
                    session.UpdateClientInfo(device, browser, ip, string.Empty);
                }

                await uow.CommitAsync();
            }
            catch
            {
            }
        });
    }
}

public static class SessionTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionTrackingMiddleware>();
    }
}
