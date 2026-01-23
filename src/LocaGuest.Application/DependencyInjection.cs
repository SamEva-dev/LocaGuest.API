using FluentValidation;
using LocaGuest.Application.Services;
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
            
            // Add Audit Behavior (logs all commands)
            cfg.AddOpenBehavior(typeof(Common.Behaviours.AuditBehavior<,>));

            cfg.AddOpenBehavior(typeof(Common.Behaviours.ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IRentabilityEngine, RentabilityEngine>();

        // AutoMapper (if needed later)
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        return services;
    }
}
