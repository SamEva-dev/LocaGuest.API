using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;

namespace LocaGuest.Infrastructure.Repositories;

public class InventoryEntryRepository : Repository<InventoryEntry>, IInventoryEntryRepository
{
    public InventoryEntryRepository(LocaGuestDbContext context) : base(context)
    {
    }
}
