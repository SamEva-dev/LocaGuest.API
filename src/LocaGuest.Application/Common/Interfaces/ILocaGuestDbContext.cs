using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LocaGuest.Application.Common.Interfaces;

public interface ILocaGuestDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<PropertyRoom> PropertyRooms { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<UserSettings> UserSettings { get; }
    DbSet<RentabilityScenario> RentabilityScenarios { get; }
    DbSet<ScenarioVersion> ScenarioVersions { get; }
    DbSet<ScenarioShare> ScenarioShares { get; }
    
    // Subscription System
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<UsageEvent> UsageEvents { get; }
    DbSet<UsageAggregate> UsageAggregates { get; }
    
    // Multi-Tenant System
    DbSet<Organization> Organizations { get; }
    DbSet<TenantSequence> TenantSequences { get; }
    
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
