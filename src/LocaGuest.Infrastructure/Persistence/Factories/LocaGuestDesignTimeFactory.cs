using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LocaGuest.Infrastructure.Persistence.Factories;

/// <summary>
/// Factory for creating LocaGuestDbContext at design time (migrations)
/// </summary>
public class LocaGuestDesignTimeFactory : IDesignTimeDbContextFactory<LocaGuestDbContext>
{
    public LocaGuestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LocaGuestDbContext>();
        
        // Use a connection string for migrations
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Locaguest;Username=postgres;Password=locaguest");

        // Create a mock mediator for design time
        var mockMediator = new MockMediator();
        
        // Create mock services (not used at design time)
        ICurrentUserService? mockCurrentUserService = null;
        ITenantContext? mockTenantContext = null;

        return new LocaGuestDbContext(optionsBuilder.Options, mockMediator, mockCurrentUserService, mockTenantContext);
    }
}

/// <summary>
/// Mock mediator for design time
/// </summary>
internal class MockMediator : IMediator
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
