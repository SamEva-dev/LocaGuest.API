using LocaGuest.Api;
using LocaGuest.Api.Middleware;
using LocaGuest.Api.Services;
using LocaGuest.Application;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LocaGuest.Api")
    .WriteTo.Console()
    .WriteTo.File("logs/locaguest-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LocaGuest API", Version = "v1" });
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

// PostgreSQL + EF Core
builder.Services.AddDbContext<LocaGuestDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Register ILocaGuestDbContext
builder.Services.AddScoped<ILocaGuestDbContext>(sp => sp.GetRequiredService<LocaGuestDbContext>());

// Repositories and UnitOfWork (DDD)
builder.Services.AddScoped<LocaGuest.Domain.Repositories.IUnitOfWork, LocaGuest.Infrastructure.Repositories.UnitOfWork>();
builder.Services.AddScoped<LocaGuest.Domain.Repositories.IPropertyRepository, LocaGuest.Infrastructure.Repositories.PropertyRepository>();
builder.Services.AddScoped<LocaGuest.Domain.Repositories.IContractRepository, LocaGuest.Infrastructure.Repositories.ContractRepository>();
builder.Services.AddScoped<LocaGuest.Domain.Repositories.ITenantRepository, LocaGuest.Infrastructure.Repositories.TenantRepository>();
builder.Services.AddScoped<LocaGuest.Domain.Repositories.ISubscriptionRepository, LocaGuest.Infrastructure.Repositories.SubscriptionRepository>();

// Stripe Service
builder.Services.AddScoped<LocaGuest.Application.Services.IStripeService, LocaGuest.Infrastructure.Services.StripeService>();

// Application Layer (includes MediatR)
builder.Services.AddApplication();

// JWT Authentication with AuthGate (RSA via JWKS)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var authGateUrl = builder.Configuration["AuthGate:Url"] ?? "https://localhost:8081";

// Cache pour les clés JWKS (éviter de recharger à chaque requête)
var jwksCache = new Dictionary<string, (List<SecurityKey> Keys, DateTime Expiry)>();
var jwksCacheLock = new object();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
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
            // La clé sera chargée dynamiquement depuis JWKS
        };
        
        // Charger dynamiquement les clés RSA depuis AuthGate JWKS
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Charger les clés RSA avec cache (5 minutes)
                List<SecurityKey>? signingKeys = null;
                
                lock (jwksCacheLock)
                {
                    if (jwksCache.TryGetValue("keys", out var cached) && cached.Expiry > DateTime.UtcNow)
                    {
                        signingKeys = cached.Keys;
                        Log.Debug("Using cached JWKS keys ({Count} keys)", signingKeys.Count);
                    }
                }
                
                if (signingKeys == null)
                {
                    try
                    {
                        Log.Information("Loading RSA keys from AuthGate JWKS: {JwksUrl}/.well-known/jwks.json", authGateUrl);
                        
                        using var httpClient = new HttpClient(new HttpClientHandler { 
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true 
                        });
                        
                        var jwksJson = httpClient.GetStringAsync($"{authGateUrl}/.well-known/jwks.json").Result;
                        var jwksDoc = System.Text.Json.JsonDocument.Parse(jwksJson);
                        var keys = jwksDoc.RootElement.GetProperty("keys").EnumerateArray();
                        
                        // Charger toutes les clés disponibles
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
                                Log.Debug("RSA key loaded (kid: {KeyId})", kid);
                            }
                        }
                        
                        if (signingKeys.Any())
                        {
                            // Mettre en cache pour 5 minutes
                            lock (jwksCacheLock)
                            {
                                jwksCache["keys"] = (signingKeys, DateTime.UtcNow.AddMinutes(5));
                            }
                            Log.Information("Loaded {Count} RSA key(s) from AuthGate JWKS", signingKeys.Count);
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
                var userId = context.Principal?.FindFirst("sub")?.Value;
                var email = context.Principal?.FindFirst("email")?.Value;
                Log.Debug("JWT Token validated - User: {UserId} ({Email})", userId, email);
                return Task.CompletedTask;
            }
        };
    });

// Fonction helper pour décoder base64url
static byte[] Base64UrlDecode(string base64Url)
{
    var base64 = base64Url.Replace('-', '+').Replace('_', '/');
    switch (base64.Length % 4)
    {
        case 2: base64 += "=="; break;
        case 3: base64 += "="; break;
    }
    return Convert.FromBase64String(base64);
}

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// Application Services
builder.Services.AddHttpContextAccessor();

// CurrentUserService implémente à la fois ICurrentUserService et ITenantContext
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<CurrentUserService>());

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LocaGuestDbContext>();

var app = builder.Build();

// Seed database in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LocaGuestDbContext>();
    await DbSeeder.SeedAsync(context);
    Log.Information("Database seeded successfully");
}

// Middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// SignalR Hubs
app.MapHub<NotificationsHub>("/hubs/notifications");

Log.Information("LocaGuest API starting...");

app.Run();
