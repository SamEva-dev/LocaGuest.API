using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using Microsoft.EntityFrameworkCore;
using LocaGuest.Domain.Aggregates.AnalyticsAggregate;

namespace LocaGuest.Application.Common.Interfaces;

public interface ILocaGuestReadDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Property> Properties { get; }
    DbSet<PropertyRoom> PropertyRooms { get; }
    DbSet<Occupant> Occupants { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractDocumentLink> ContractDocumentLinks { get; }
    DbSet<Document> Documents { get; }
    DbSet<Payment> Payments { get; }
    DbSet<InventoryEntry> InventoryEntries { get; }
    DbSet<InventoryExit> InventoryExits { get; }

    DbSet<SatisfactionFeedback> SatisfactionFeedbacks { get; }

    // Intentionally no SaveChangesAsync: query-side only.
}
