using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;

namespace LocaGuest.Infrastructure.Repositories;

public class InventoryExitRepository : Repository<InventoryExit>, IInventoryExitRepository
{
    public InventoryExitRepository(LocaGuestDbContext context) : base(context)
    {
    }
}
