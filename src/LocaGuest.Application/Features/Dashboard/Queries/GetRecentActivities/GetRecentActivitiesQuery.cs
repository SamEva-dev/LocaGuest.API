using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetRecentActivities;

public record GetRecentActivitiesQuery : IRequest<Result<List<ActivityDto>>>
{
    public int Limit { get; init; } = 20;
}

public class ActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
