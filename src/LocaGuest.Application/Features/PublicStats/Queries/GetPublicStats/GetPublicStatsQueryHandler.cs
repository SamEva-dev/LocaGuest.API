using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.PublicStats.Queries.GetPublicStats;

public class GetPublicStatsQueryHandler : IRequestHandler<GetPublicStatsQuery, Result<PublicStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPublicStatsQueryHandler> _logger;

    public GetPublicStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPublicStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PublicStatsDto>> Handle(GetPublicStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var averageRating = await _unitOfWork.SatisfactionFeedbacks.Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Select(x => (double?)x.Rating)
                .AverageAsync(cancellationToken);

            var propertiesCount = await _unitOfWork.Properties.Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Count all occupants/tenants as users
            var occupantsCount = await _unitOfWork.Occupants.Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Public endpoint: must work without tenant context
            var organizationsCount = await _unitOfWork.Organizations.Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var avg = averageRating ?? 4.8; // fallback if no votes yet
            avg = Math.Clamp(avg, 1.0, 5.0);

            var satisfactionRate = (int)Math.Round((avg / 5.0) * 100.0);

            var stats = new PublicStatsDto
            {
                PropertiesCount = propertiesCount,
                UsersCount = occupantsCount,
                SatisfactionRate = satisfactionRate,
                OrganizationsCount = organizationsCount,
                AverageRating = Math.Round(avg, 1)
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public statistics");
            return Result.Failure<PublicStatsDto>("Error retrieving public statistics");
        }
    }
}
