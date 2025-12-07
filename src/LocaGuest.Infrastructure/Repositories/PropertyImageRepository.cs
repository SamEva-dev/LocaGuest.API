using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;

namespace LocaGuest.Infrastructure.Repositories;

public class PropertyImageRepository : Repository<PropertyImage>, IPropertyImageRepository
{
    public PropertyImageRepository(LocaGuestDbContext context) : base(context)
    {
    }
}
