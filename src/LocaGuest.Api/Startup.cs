using LocaGuest.Api.Middleware;
using LocaGuest.Api.Services;
using LocaGuest.Application;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure;
using LocaGuest.Infrastructure.Persistence.Seeders;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LocaGuest.Infrastructure.BackgroundServices;
using LocaGuest.Api.Common;
using LocaGuest.Infrastructure.Jobs;
using LocaGuest.Api.Common.Swagger;
using LocaGuest.Api.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace LocaGuest.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Controllers with JSON configuration
        services.AddControllers(options =>
            {
                options.Conventions.Add(new VersionedApiRouteConvention("v1"));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: "LocaGuest.Api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                var otlpEndpoint = Configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddPrometheusExporter();
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var correlationId = context.HttpContext.Items["X-Correlation-Id"]?.ToString();
                if (string.IsNullOrWhiteSpace(correlationId))
                    correlationId = context.HttpContext.TraceIdentifier;

                var errors = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };

                problem.Extensions["correlationId"] = correlationId;

                return new ObjectResult(problem)
                {
                    StatusCode = problem.Status,
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "LocaGuest API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName);
            c.OperationFilter<IdempotencyKeyOperationFilter>();
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
        services.AddScoped<LocaGuest.Domain.Repositories.IOccupantRepository, LocaGuest.Infrastructure.Repositories.TenantRepository>();
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

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

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

            options.AddPolicy(Permissions.PropertiesRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.PropertiesRead)));
            options.AddPolicy(Permissions.PropertiesWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.PropertiesWrite)));

            options.AddPolicy(Permissions.RoomsRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.RoomsRead)));
            options.AddPolicy(Permissions.RoomsWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.RoomsWrite)));

            options.AddPolicy(Permissions.ContractsRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.ContractsRead)));
            options.AddPolicy(Permissions.ContractsWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.ContractsWrite)));

            options.AddPolicy(Permissions.PaymentsRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.PaymentsRead)));
            options.AddPolicy(Permissions.PaymentsWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.PaymentsWrite)));

            options.AddPolicy(Permissions.DepositsRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.DepositsRead)));
            options.AddPolicy(Permissions.DepositsWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.DepositsWrite)));

            options.AddPolicy(Permissions.DocumentsRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.DocumentsRead)));
            options.AddPolicy(Permissions.DocumentsWrite, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.DocumentsWrite)));

            options.AddPolicy(Permissions.TeamRead, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.TeamRead)));
            options.AddPolicy(Permissions.TeamManage, policy =>
                policy.RequireAuthenticatedUser().RequireClaim("organization_id").AddRequirements(new PermissionRequirement(Permissions.TeamManage)));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var method = httpContext.Request.Method;
                if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
                    return RateLimitPartition.GetNoLimiter("read");

                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/ready", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/live", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetNoLimiter("infra");
                }

                var user = httpContext.User;

                var organizationId =
                    user.FindFirst("organization_id")?.Value
                    ?? user.FindFirst("organizationId")?.Value;

                var partitionKey = !string.IsNullOrWhiteSpace(organizationId)
                    ? $"org:{organizationId}"
                    : user.FindFirst("azp")?.Value
                      ?? user.FindFirst("client_id")?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                      ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

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

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
        var allowInsecureJwksTls = string.Equals(envName, Environments.Development, StringComparison.OrdinalIgnoreCase)
            || string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase);

        var jwksClient = services.AddHttpClient(nameof(AuthGateJwksProvider));

        if (allowInsecureJwksTls)
        {
            jwksClient.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });
        }

        services.AddSingleton<AuthGateJwksProvider>();

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
                        var jwksProvider = context.HttpContext.RequestServices.GetRequiredService<AuthGateJwksProvider>();
                        context.Options.TokenValidationParameters.IssuerSigningKeyResolver =
                            (token, securityToken, kid, validationParameters) => jwksProvider.GetSigningKeys(kid);

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
                            Log.Debug("JWT Token validated - UserId={UserId}", userId);
                        }
                        return Task.CompletedTask;
                    }
                };
            });
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

        var forceHttpsConfigured = Configuration.GetValue<bool?>("Security:ForceHttps");
        var forceHttps = forceHttpsConfigured ?? env.IsProduction();
        if (forceHttps)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
                context.Response.Headers.TryAdd("Content-Security-Policy", "frame-ancestors 'none';");
                return Task.CompletedTask;
            });

            await next();
        });

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

                Log.Information("Applying Audit_Locaguest database migrations...");
                auditContext.Database.Migrate();

                var seedPlansConfigured = Configuration.GetValue<bool?>("Billing:SeedPlans");
                var seedPlans = seedPlansConfigured ?? (env.IsDevelopment() || env.EnvironmentName == "Testing");
                if (seedPlans)
                {
                    PlanSeeder.SeedPlansAsync(context).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while applying migrations");
                throw;
            }
        }

        // Middleware
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseObservabilityEnrichment();
        app.UseMiddleware<IdempotencyMiddleware>();
        if (!env.IsDevelopment() && env.IsStaging())
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        app.UseSerilogRequestLogging();

        if (env.IsDevelopment() || env.IsStaging())
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

            endpoints.MapPrometheusScrapingEndpoint("/metrics").RequireAuthorization();

            // SignalR Hubs
            endpoints.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization("RequireOrganization");

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
