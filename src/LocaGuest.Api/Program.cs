using LocaGuest.Api;
using LocaGuest.Api.Middleware;
using LocaGuest.Api.Services;
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

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// JWT Authentication with AuthGate (RSA via JWKS)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var authGateUrl = builder.Configuration["AuthGate:Url"] ?? "https://localhost:8081";

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
                // Charger les clés RSA lors de la première requête
                if (context.Options.TokenValidationParameters.IssuerSigningKey == null)
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
                                
                                context.Options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = kid };
                                Log.Information("RSA key loaded successfully (kid: {KeyId})", kid);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to load RSA keys from AuthGate JWKS");
                    }
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
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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
