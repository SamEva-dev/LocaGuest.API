using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.PublicStats.Queries.GetPublicStats;

public record GetPublicStatsQuery : IRequest<Result<PublicStatsDto>>;

public class PublicStatsDto
{
    public int PropertiesCount { get; set; }
    public int UsersCount { get; set; }
    public int SatisfactionRate { get; set; }
    public int OrganizationsCount { get; set; }
    public double AverageRating { get; set; }
}
