using LocaGuest.Api;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

try
{
    Log.Information("*** STARTUP ***");

    var builder = WebApplication.CreateBuilder(args);

    static string ResolveHomeDirectory(string envVarName, string appFolderName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        // Cross-OS fallback
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = AppContext.BaseDirectory;
        }

        var resolved = Path.Combine(baseDir, appFolderName);

        // Best effort: set for current process so Serilog config / other code can reuse it.
        Environment.SetEnvironmentVariable(envVarName, resolved, EnvironmentVariableTarget.Process);

        return resolved;
    }

    static void ConfigureSerilogFilePaths(WebApplicationBuilder b, string homeEnvVar, string appFolder)
    {
        var home = ResolveHomeDirectory(homeEnvVar, appFolder);

        var appLogPath = Path.Combine(home, "log", "Locaguest", "LocaguestApi_log.txt");
        var efLogPath = Path.Combine(home, "log", "Locaguest", "EntityFramework", "EntityFramework_log.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(appLogPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(efLogPath)!);

        b.Configuration["Serilog:WriteTo:1:Args:configureLogger:WriteTo:0:Args:path"] = appLogPath;
        b.Configuration["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = efLogPath;
    }

    ConfigureSerilogFilePaths(builder, "LOCAGUEST_HOME", "LocaGuest");

    static string RedactPostgresConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        // Best-effort redaction for logs
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(?i)(password)\s*=\s*[^;]+",
            "$1=***");
    }

    static void LogEffectiveDatabaseTarget(WebApplicationBuilder b)
    {
        var provider = b.Configuration["Database:Provider"]?.ToLowerInvariant() ?? "postgresql";
        if (provider == "sqlite")
        {
            Log.Warning("Database Provider={Provider}; SQLite is no longer supported", provider);
            return;
        }

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            try
            {
                var uri = new Uri(databaseUrl.Split('?')[0]);
                var dbName = uri.AbsolutePath.TrimStart('/');
                Log.Information("Database Provider={Provider}; DATABASE_URL host={Host}; port={Port}; db={Db}", provider, uri.Host, uri.Port, dbName);
                return;
            }
            catch
            {
                Log.Warning("Database Provider={Provider}; DATABASE_URL is set but could not be parsed", provider);
            }
        }

        var pg = b.Configuration.GetConnectionString("DefaultConnection_Locaguest") ?? string.Empty;
        Log.Information("Database Provider={Provider}; ConnectionString(DefaultConnection_Locaguest)={ConnectionString}", provider, RedactPostgresConnectionString(pg));
    }

    // Serilog (from appsettings + env)
    builder.Host.UseSerilog((ctx, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services));

    LogEffectiveDatabaseTarget(builder);

    // Use Startup class for service configuration
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    // Use Startup class for middleware configuration
    startup.Configure(app, app.Environment);

    Log.Information("LocaGuest API starting...");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
