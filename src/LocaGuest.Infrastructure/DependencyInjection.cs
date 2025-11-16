using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace LocaGuest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "sqlite";

        // Main Database (LocaGuest)
        services.AddDbContext<LocaGuestDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteConnection") ?? "Data Source=./Data/LocaGuest.db",
                    b => b.MigrationsAssembly(typeof(LocaGuestDbContext).Assembly.FullName));
            }
            else
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    b => b.MigrationsAssembly(typeof(LocaGuestDbContext).Assembly.FullName));
            }
        });

        // Audit Database
        services.AddDbContext<AuditDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteAuditConnection") ?? "Data Source=./Data/LocaGuest_Audit.db",
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));
            }
            else
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("Audit"),
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));
            }
        });

        // Multi-Tenant Services  
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<INumberSequenceService, NumberSequenceService>();

        return services;
    }
}
