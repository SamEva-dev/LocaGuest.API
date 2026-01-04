using LocaGuest.Api.Middleware;
using LocaGuest.Api.Services;
using LocaGuest.Application;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LocaGuest.Infrastructure.Jobs;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace LocaGuest.Api;

public class Startup
{
    private readonly Dictionary<string, (List<SecurityKey> Keys, DateTime Expiry)> _jwksCache = new();
    private readonly object _jwksCacheLock = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Controllers with JSON configuration
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "LocaGuest API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName);
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Infrastructure Layer
        services.AddInfrastructure(Configuration);

        // Repositories and UnitOfWork
        services.AddScoped<LocaGuest.Domain.Repositories.IUnitOfWork, LocaGuest.Infrastructure.Repositories.UnitOfWork>();
        services.AddScoped<LocaGuest.Domain.Repositories.IPropertyRepository, LocaGuest.Infrastructure.Repositories.PropertyRepository>();
        services.AddScoped<LocaGuest.Domain.Repositories.IContractRepository, LocaGuest.Infrastructure.Repositories.ContractRepository>();
        services.AddScoped<LocaGuest.Domain.Repositories.ITenantRepository, LocaGuest.Infrastructure.Repositories.TenantRepository>();
        services.AddScoped<LocaGuest.Domain.Repositories.ISubscriptionRepository, LocaGuest.Infrastructure.Repositories.SubscriptionRepository>();

        // Application Services
        services.AddScoped<LocaGuest.Application.Services.IStripeService, LocaGuest.Infrastructure.Services.StripeService>();
        services.AddScoped<LocaGuest.Application.Services.IAuditService, LocaGuest.Infrastructure.Services.AuditService>();
        services.AddScoped<LocaGuest.Application.Services.ITrackingService, LocaGuest.Infrastructure.Services.TrackingService>();
        services.AddScoped<LocaGuest.Application.Services.IEffectiveContractStateResolver, LocaGuest.Application.Services.EffectiveContractStateResolver>();

        // Email Service
        services.Configure<LocaGuest.Infrastructure.Email.EmailSettings>(Configuration.GetSection("EmailSettings"));
        services.AddScoped<LocaGuest.Application.Common.Interfaces.IEmailService, LocaGuest.Infrastructure.Email.EmailService>();

        // Application Layer (MediatR)
        services.AddApplication();

        // Background Services
        services.AddHostedService<LocaGuest.Infrastructure.BackgroundServices.EmailNotificationBackgroundService>();
        services.AddHostedService<LocaGuest.Infrastructure.BackgroundServices.InvoiceGenerationBackgroundService>();
        services.AddHostedService<LocaGuest.Infrastructure.BackgroundServices.ContractActivationBackgroundService>();

        services.Configure<AuditRetentionOptions>(Configuration.GetSection("AuditRetention"));
        services.AddHostedService<AuditRetentionHostedService>();

        // JWT Authentication with AuthGate (RSA via JWKS)
        ConfigureJwtAuthentication(services);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireOrganization", policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id"));

            // Fail-closed by default: any authenticated request without organization_id is forbidden.
            // Endpoints that must remain public should use [AllowAnonymous] or be configured with AllowAnonymous.
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("organization_id")
                .Build();
            options.FallbackPolicy = options.DefaultPolicy;

            options.AddPolicy("Provisioning", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                {
                    var scope = ctx.User.FindFirst("scope")?.Value ?? string.Empty;
                    return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Contains("locaguest.provisioning");
                });
            });

            options.AddPolicy("SuperAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                    ctx.User.FindAll("roles").Any(r => r.Value == "SuperAdmin") ||
                    ctx.User.FindAll(ClaimTypes.Role).Any(r => r.Value == "SuperAdmin"));
            });
        });

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("ProvisioningLimiter", httpContext =>
            {
                var user = httpContext.User;
                var clientId =
                    user.FindFirst("azp")?.Value
                    ?? user.FindFirst("client_id")?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        // CORS
        services.AddCors(options =>
        {
            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
            options.AddPolicy("AllowAngular", policy =>
            {
                var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // SignalR
        services.AddSignalR();

        // HttpContext and User Context
        services.AddHttpContextAccessor();
        services.AddScoped<OrganizationContextAccessor>();
        services.AddScoped<IOrganizationContext>(sp => sp.GetRequiredService<OrganizationContextAccessor>());
        services.AddScoped<IOrganizationContextWriter>(sp => sp.GetRequiredService<OrganizationContextAccessor>());
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<LocaGuestDbContext>("database", tags: new[] { "ready" })
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" });
    }

    private void ConfigureJwtAuthentication(IServiceCollection services)
    {
        var jwtSettings = Configuration.GetSection("Jwt");
        var authGateUrl = Configuration["AuthGate:Url"] ?? "https://localhost:8081";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "AuthGate",
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"] ?? "AuthGate",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        List<SecurityKey>? signingKeys = null;

                        lock (_jwksCacheLock)
                        {
                            if (_jwksCache.TryGetValue("keys", out var cached) && cached.Expiry > DateTime.UtcNow)
                            {
                                signingKeys = cached.Keys;
                            }
                        }

                        if (signingKeys == null)
                        {
                            try
                            {
                                using var httpClient = new HttpClient(new HttpClientHandler
                                {
                                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                                });

                                var jwksJson = httpClient.GetStringAsync($"{authGateUrl}/.well-known/jwks.json").Result;
                                var jwksDoc = System.Text.Json.JsonDocument.Parse(jwksJson);
                                var keys = jwksDoc.RootElement.GetProperty("keys").EnumerateArray();

                                signingKeys = new List<SecurityKey>();
                                foreach (var key in keys)
                                {
                                    var n = key.GetProperty("n").GetString();
                                    var e = key.GetProperty("e").GetString();
                                    var kid = key.GetProperty("kid").GetString();

                                    if (n != null && e != null)
                                    {
                                        var nBytes = Base64UrlDecode(n);
                                        var eBytes = Base64UrlDecode(e);

                                        var rsa = RSA.Create();
                                        rsa.ImportParameters(new RSAParameters
                                        {
                                            Modulus = nBytes,
                                            Exponent = eBytes
                                        });

                                        signingKeys.Add(new RsaSecurityKey(rsa) { KeyId = kid });
                                    }
                                }

                                if (signingKeys.Any())
                                {
                                    lock (_jwksCacheLock)
                                    {
                                        _jwksCache["keys"] = (signingKeys, DateTime.UtcNow.AddMinutes(5));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to load RSA keys from AuthGate JWKS");
                            }
                        }

                        if (signingKeys != null && signingKeys.Any())
                        {
                            context.Options.TokenValidationParameters.IssuerSigningKeys = signingKeys;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var principal = context.Principal;

                        var userId = principal?.FindFirst("sub")?.Value
                                     ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? principal?.FindFirst("nameid")?.Value
                                     ?? principal?.FindFirst("userId")?.Value;

                        var email = principal?.FindFirst("email")?.Value
                                    ?? principal?.FindFirst(ClaimTypes.Email)?.Value
                                    ?? principal?.FindFirst("upn")?.Value;

                        // Avoid log spam when tokens don't carry these claims
                        if (!string.IsNullOrWhiteSpace(userId) || !string.IsNullOrWhiteSpace(email))
                        {
                            Log.Debug("JWT Token validated - User: {UserId} ({Email})", userId, email);
                        }
                        return Task.CompletedTask;
                    }
                };
            });
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        EnsureLocaguestLogDirectories();
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (env.IsDevelopment() || env.EnvironmentName == "Testing")
        {
            app.UseDeveloperExceptionPage();
        }

        // Apply migrations in Development and Production
        if (env.IsDevelopment() || env.IsProduction())
        {
            using var scope = app.ApplicationServices.CreateScope();

            try
            {
                var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
                var auditContext = scope.ServiceProvider.GetRequiredService<LocaGuest.Infrastructure.Persistence.AuditDbContext>();

                Log.Information("Applying LocaGuest database migrations...");
                context.Database.Migrate();
                Log.Information("LocaGuest database migrations applied successfully");

                Log.Information("Applying Audit database migrations...");
                auditContext.Database.Migrate();

                Log.Information("⚠️ Database seeding disabled - Empty database");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while applying migrations");
                throw;
            }
        }

        // Middleware
        app.UseMiddleware<CorrelationIdMiddleware>();
        if (!env.IsDevelopment() && env.EnvironmentName != "Testing")
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        var swaggerEnabled = Configuration.GetValue<bool>("Swagger:Enabled");
        if (swaggerEnabled || env.IsDevelopment() || env.EnvironmentName == "Testing")
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors("AllowAngular");

        app.UseAuthentication();
        app.UseMiddleware<OrganizationContextMiddleware>();
        app.UseAuthorization();

        app.UseRateLimiter();

        // Tracking middleware (after authentication)
        app.UseTracking();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            // SignalR Hubs
            endpoints.MapHub<NotificationsHub>("/hubs/notifications");

            // Health Check Endpoints
            endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        timestamp = DateTime.UtcNow,
                        service = "LocaGuest.API",
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            }).AllowAnonymous();

            endpoints.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            }).AllowAnonymous();

            endpoints.MapHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            }).AllowAnonymous();
        });
    }

    private void EnsureLocaguestLogDirectories()
    {
        // 1) Récupère la variable
        var home = Environment.GetEnvironmentVariable("LOCAGUEST_HOME");

        // 2) Si elle n'existe pas au runtime, on la force (au niveau Process)
        //    Important : mets bien un trailing backslash : E:\ (pas E:)
        if (string.IsNullOrWhiteSpace(home))
        {
            home = @"E:\";
            Environment.SetEnvironmentVariable("LOCAGUEST_HOME", home, EnvironmentVariableTarget.Process);
        }

        // 3) Crée les dossiers attendus par tes sinks fichier
        var locaGuestDir = Path.Combine(home, "log", "LocaGuest");
        var efDir = Path.Combine(locaGuestDir, "EntityFramework");

        Directory.CreateDirectory(locaGuestDir);
        Directory.CreateDirectory(efDir);
    }
}
