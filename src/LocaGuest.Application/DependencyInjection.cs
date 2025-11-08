using Microsoft.Extensions.DependencyInjection;

namespace LocaGuest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // FluentValidation (if needed later)
        // services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // AutoMapper (if needed later)
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        return services;
    }
}
