using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Common.Interfaces;

public interface ILocaGuestReadDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<PropertyRoom> PropertyRooms { get; }
    DbSet<Occupant> Occupants { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<Document> Documents { get; }
    DbSet<Payment> Payments { get; }
    DbSet<InventoryEntry> InventoryEntries { get; }
    DbSet<InventoryExit> InventoryExits { get; }

    // Intentionally no SaveChangesAsync: query-side only.
}
