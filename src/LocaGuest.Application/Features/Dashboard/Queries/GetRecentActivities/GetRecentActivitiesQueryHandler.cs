using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRecentActivities;

public class GetRecentActivitiesQueryHandler : IRequestHandler<GetRecentActivitiesQuery, Result<List<ActivityDto>>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetRecentActivitiesQueryHandler> _logger;

    public GetRecentActivitiesQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetRecentActivitiesQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<List<ActivityDto>>> Handle(GetRecentActivitiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var activities = new List<ActivityDto>();

            // Get recent contracts
            var recentContracts = await _readDb.Contracts.AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var contract in recentContracts)
            {
                activities.Add(new ActivityDto
                {
                    Type = "success",
                    Title = $"Nouveau contrat créé",
                    Date = contract.CreatedAt
                });
            }

            // Get recent tenants
            var recentTenants = await _readDb.Occupants.AsNoTracking()
                .Where(t => t.Status == OccupantStatus.Active)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var tenant in recentTenants)
            {
                activities.Add(new ActivityDto
                {
                    Type = "info",
                    Title = $"Nouveau locataire: {tenant.FullName}",
                    Date = tenant.CreatedAt
                });
            }

            // Get recent properties
            var recentProperties = await _readDb.Properties.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var property in recentProperties)
            {
                activities.Add(new ActivityDto
                {
                    Type = "info",
                    Title = $"Nouveau bien: {property.Name}",
                    Date = property.CreatedAt
                });
            }

            // Sort all activities by date and take limit
            var sortedActivities = activities
                .OrderByDescending(a => a.Date)
                .Take(request.Limit)
                .ToList();

            return Result.Success(sortedActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activities");
            return Result.Failure<List<ActivityDto>>("Error retrieving recent activities");
        }
    }
}
