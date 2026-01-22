using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Services;
using LocaGuest.Infrastructure.Services.ContractGenerator;
using LocaGuest.Infrastructure.Services.PropertySheetGenerator;
using LocaGuest.Infrastructure.Services.QuittanceGenerator;
using LocaGuest.Infrastructure.Services.OccupantSheetGenerator;
using LocaGuest.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LocaGuest.Infrastructure.Services.InvoicePdfGenerator;
using LocaGuest.Emailing.Registration;
using LocaGuest.Emailing.Workers;

namespace LocaGuest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "postgresql";

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        var isDevOrTesting = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase);
        var sslMode = isDevOrTesting ? "SSL Mode=Allow" : "SSL Mode=Require";

        services.AddScoped<AuditSaveChangesInterceptor>();

        // Main Database (LocaGuest)
        services.AddDbContext<LocaGuestDbContext>((sp, options) =>
        {
            if (provider == "sqlite")
            {
                throw new InvalidOperationException("SQLite is no longer supported. Set Database:Provider to 'postgresql'.");
            }
            else
            {
                // Use DATABASE_URL environment variable, or fallback to DefaultConnection_Locaguest
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};{sslMode}";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("DefaultConnection_Locaguest");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(LocaGuestDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // LocaGuest.Emailing (queue + worker) - uses same Postgres DB
        services.AddLocaGuestEmailing(configuration, db =>
        {
            // Use DATABASE_URL environment variable, or fallback to DefaultConnection_Locaguest
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            string connectionString;

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                var userInfo = uri.UserInfo.Split(':');
                connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};{sslMode}";
            }
            else
            {
                connectionString = configuration.GetConnectionString("DefaultConnection_Locaguest");
            }

            db.UsePostgres(connectionString, migrationsAssembly: typeof(DependencyInjection).Assembly.FullName);
        });
        services.AddHostedService<EmailDispatcherWorker>();

        // Audit_Locaguest Database
        services.AddDbContext<AuditDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                throw new InvalidOperationException("SQLite is no longer supported. Set Database:Provider to 'postgresql'.");
            }
            else
            {
                // Use DATABASE_URL environment variable, or fallback to Audit_Locaguest
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    var dbName = uri.AbsolutePath.TrimStart('/') + "_audit";
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};{sslMode}";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("Audit_Locaguest");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName))
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        });

        // Register ILocaGuestDbContext
        services.AddScoped<ILocaGuestDbContext>(sp => sp.GetRequiredService<LocaGuestDbContext>());

        // Query-side DbContext abstraction (CQRS)
        services.AddScoped<ILocaGuestReadDbContext>(sp => sp.GetRequiredService<LocaGuestDbContext>());

        // Register IAuditDbContext
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());

        // HttpClient for AuthGate
        services.AddScoped<ForwardAuthorizationHeaderHandler>();

        services.AddHttpClient("AuthGateApi", client =>
        {
            var baseUrl = configuration["HttpClients:AuthGateApi:BaseUrl"] ?? "https://localhost:8081";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

        services.AddHttpClient<IAuthGateClient, AuthGateClient>(client =>
        {
            var baseUrl = configuration["HttpClients:AuthGateApi:BaseUrl"] ?? "https://localhost:8081";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();
        
        // Multi-Tenant Services  
        services.AddScoped<INumberSequenceService, NumberSequenceService>();

        // Document Services
        services.AddScoped<IContractGeneratorService, ContractGeneratorService>();
        services.AddScoped<IQuittanceGeneratorService, QuittanceGeneratorService>();
        services.AddScoped<IInvoicePdfGeneratorService, InvoicePdfGeneratorService>();
        services.AddScoped<IPropertySheetGeneratorService, PropertySheetGeneratorService>();
        services.AddScoped<IOccupantSheetGeneratorService, OccupantSheetGeneratorService>();
        
        // File Storage Service
        services.AddScoped<IFileStorageService, FileStorageService>();

        services.AddScoped<IAdminMaintenanceService, AdminMaintenanceService>();

        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        services.AddScoped<IProvisioningService, ProvisioningService>();
        services.AddScoped<IInvitationProvisioningService, InvitationProvisioningService>();

        return services;
    }
}
