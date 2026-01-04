using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Email;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Services;
using LocaGuest.Infrastructure.Services.ContractGenerator;
using LocaGuest.Infrastructure.Services.PropertySheetGenerator;
using LocaGuest.Infrastructure.Services.QuittanceGenerator;
using LocaGuest.Infrastructure.Services.TenantSheetGenerator;
using LocaGuest.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LocaGuest.Infrastructure.Services.InvoicePdfGenerator;

namespace LocaGuest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "sqlite";

        services.AddScoped<AuditSaveChangesInterceptor>();

        // Main Database (LocaGuest)
        services.AddDbContext<LocaGuestDbContext>((sp, options) =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteConnection") ?? "Data Source=./Data/LocaGuest.db",
                    b => b.MigrationsAssembly(typeof(LocaGuestDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
            else
            {
                // Use DATABASE_URL from Fly.io, or fallback to Default
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("Default");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(LocaGuestDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Audit Database
        services.AddDbContext<AuditDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteAuditConnection") ?? "Data Source=./Data/LocaGuest_Audit.db",
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
            else
            {
                // Use DATABASE_URL from Fly.io, or fallback to Audit
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    var dbName = uri.AbsolutePath.TrimStart('/') + "_audit";
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("Audit");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        });

        // Register ILocaGuestDbContext
        services.AddScoped<ILocaGuestDbContext>(sp => sp.GetRequiredService<LocaGuestDbContext>());

        // Register IAuditDbContext
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());

        // HttpClient for AuthGate
        services.AddHttpClient("AuthGateApi", client =>
        {
            var baseUrl = configuration["HttpClients:AuthGateApi:BaseUrl"] ?? "https://localhost:8081";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Multi-Tenant Services  
        services.AddScoped<INumberSequenceService, NumberSequenceService>();

        // Document Services
        services.AddScoped<IContractGeneratorService, ContractGeneratorService>();
        services.AddScoped<IQuittanceGeneratorService, QuittanceGeneratorService>();
        services.AddScoped<IInvoicePdfGeneratorService, InvoicePdfGeneratorService>();
        services.AddScoped<IPropertySheetGeneratorService, PropertySheetGeneratorService>();
        services.AddScoped<ITenantSheetGeneratorService, TenantSheetGeneratorService>();
        
        // Email Service
        services.AddScoped<IEmailService, LocaGuest.Infrastructure.Email.EmailService>();
        
        // File Storage Service
        services.AddScoped<IFileStorageService, FileStorageService>();

        services.AddScoped<IAdminMaintenanceService, AdminMaintenanceService>();

        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        services.AddScoped<IProvisioningService, ProvisioningService>();
        services.AddScoped<IInvitationProvisioningService, InvitationProvisioningService>();

        return services;
    }
}
