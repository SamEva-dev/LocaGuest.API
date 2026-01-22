using System;
using LocaGuest.Emailing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LocaGuest.Infrastructure.Persistence;

public sealed class EmailingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EmailingDbContext>
{
    public EmailingDbContext CreateDbContext(string[] args)
    {
        // Reuse same rules as runtime:
        // - DATABASE_URL if present (preferred)
        // - otherwise ConnectionStrings:DefaultConnection_Locaguest from appsettings
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        var isDevOrTesting = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase);
        var sslMode = isDevOrTesting ? "SSL Mode=Allow" : "SSL Mode=Require";

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        string connectionString;

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            var uri = new Uri(databaseUrl.Split('?')[0]);
            var userInfo = uri.UserInfo.Split(':');
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};{sslMode}";
        }
        else
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{envName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            connectionString = configuration.GetConnectionString("DefaultConnection_Locaguest")
                ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection_Locaguest'.");
        }

        var options = new DbContextOptionsBuilder<EmailingDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName))
            .Options;

        return new EmailingDbContext(options);
    }
}
