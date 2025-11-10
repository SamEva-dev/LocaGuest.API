using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using Microsoft.EntityFrameworkCore;
using LocaGuest.Domain.Aggregates.UserAggregate;

namespace LocaGuest.Application.Common.Interfaces;

public interface ILocaGuestDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<UserSettings> UserSettings { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
