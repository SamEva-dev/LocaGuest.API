using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SerilogTracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Diagnostics;
using LocaGuest.Presentation;

var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", false, true);

IConfiguration configuration = configurationBuilder.Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console()
    .ReadFrom.Configuration(configuration)
    .Filter.ByExcluding(evt =>
        evt.Properties.ContainsKey("transcriptPath") ||
        evt.Properties.ContainsKey("audioPath") ||
        evt.Properties.ContainsKey("summaryPath")
    )
    .CreateLogger();

using var listener = new ActivityListenerConfiguration()
    .Instrument.AspNetCoreRequests()
    .TraceToSharedLogger();
if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");


Log.Information("*** STARTUP ***");
var requiredPythonLibs = new[] { "torch",
                                "faster-whisper",
                                "pyannote.audio",
                                "sentencepiece",
                                "python-multipart",
                                "pydantic",
                                "langdetect"};



var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(configuration!);
                if (configuration == null)
                {
                    throw new InvalidOperationException("Configuration not initialized");
                }
                //services.AddApplication();
               // services.AddInfrastructure(configuration);
            })
            .ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddConsole();
                logger.AddSerilog(dispose: true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    options.ListenAnyIP(5000, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                });
                webBuilder.UseStartup<Startup>();
            })
            .UseSerilog()
            .Build();

//MigrationManager.ApplyMigrations(host);

//FastApiWorkerManager.StartAndMonitor(configuration, host);

host.Run();