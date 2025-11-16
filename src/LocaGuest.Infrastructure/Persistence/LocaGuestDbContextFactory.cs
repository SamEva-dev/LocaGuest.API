using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MediatR;

namespace LocaGuest.Infrastructure.Persistence;

/// <summary>
/// Factory for creating LocaGuestDbContext instances at design time (for migrations)
/// Reads provider configuration from appsettings.json
/// </summary>
public class LocaGuestDbContextFactory : IDesignTimeDbContextFactory<LocaGuestDbContext>
{
    public LocaGuestDbContext CreateDbContext(string[] args)
    {
        // ✅ Read configuration from appsettings.json (like runtime)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LocaGuest.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<LocaGuestDbContext>();
        
        // ✅ Respect the provider from configuration
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "sqlite";
        
        if (provider == "npgsql" || provider == "postgresql")
        {
            var connectionString = configuration.GetConnectionString("Default");
            optionsBuilder.UseNpgsql(connectionString);
            Console.WriteLine($"[DesignTime] Using PostgreSQL: {connectionString}");
        }
        else
        {
            var connectionString = configuration.GetConnectionString("SqliteConnection");
            optionsBuilder.UseSqlite(connectionString);
            Console.WriteLine($"[DesignTime] Using SQLite: {connectionString}");
        }
        
        // Create a dummy mediator for design time
        var mediator = new NoMediator();
        
        return new LocaGuestDbContext(optionsBuilder.Options, mediator);
    }
    
    // Dummy mediator for design time
    private class NoMediator : IMediator
    {
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
