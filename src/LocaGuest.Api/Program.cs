using LocaGuest.Api;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LocaGuest.Api")
    .WriteTo.Console()
    .WriteTo.File("logs/locaguest-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Use Startup class for service configuration
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Use Startup class for middleware configuration
startup.Configure(app, app.Environment);

Log.Information("LocaGuest API starting...");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
