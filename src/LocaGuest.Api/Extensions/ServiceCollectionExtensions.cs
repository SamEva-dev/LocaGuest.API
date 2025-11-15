using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;

namespace LocaGuest.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        
        return services;
    }
}
