using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetDeadlines;

public record GetDeadlinesQuery : IRequest<Result<DeadlinesDto>>
{
}

public record DeadlinesDto
{
    public List<DeadlineItem> UpcomingDeadlines { get; init; } = new();
}

public record DeadlineItem
{
    public string Type { get; init; } = string.Empty; // "Rent", "Contract", "Maintenance"
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string PropertyCode { get; init; } = string.Empty;
    public string OccupantName { get; init; } = string.Empty;
}
