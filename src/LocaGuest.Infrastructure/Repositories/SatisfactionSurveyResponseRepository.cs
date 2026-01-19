using LocaGuest.Domain.Aggregates.AnalyticsAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;

namespace LocaGuest.Infrastructure.Repositories;

public class SatisfactionFeedbackRepository : Repository<SatisfactionFeedback>, ISatisfactionFeedbackRepository
{
    public SatisfactionFeedbackRepository(LocaGuestDbContext context) : base(context)
    {
    }
}
