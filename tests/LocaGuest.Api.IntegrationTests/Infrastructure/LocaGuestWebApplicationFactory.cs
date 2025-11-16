using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LocaGuest.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Uses In-Memory database and configures test environment
/// </summary>
public class LocaGuestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace JWT authentication with test authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
        });

        // Set Testing environment - Program.cs will configure InMemory DB automatically
        builder.UseEnvironment("Testing");
    }
}
