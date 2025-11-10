using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rentability.Queries.GetScenarioVersions;

public class GetScenarioVersionsQueryHandler : IRequestHandler<GetScenarioVersionsQuery, Result<List<ScenarioVersionDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetScenarioVersionsQueryHandler> _logger;

    public GetScenarioVersionsQueryHandler(
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetScenarioVersionsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<ScenarioVersionDto>>> Handle(GetScenarioVersionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);

            // Verify user owns the scenario
            var scenario = await _context.RentabilityScenarios
                .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == userId, cancellationToken);

            if (scenario == null)
            {
                return Result.Failure<List<ScenarioVersionDto>>("Scenario not found");
            }

            var versions = await _context.ScenarioVersions
                .Where(v => v.ScenarioId == request.ScenarioId)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new ScenarioVersionDto
                {
                    Id = v.Id,
                    ScenarioId = v.ScenarioId,
                    VersionNumber = v.VersionNumber,
                    ChangeDescription = v.ChangeDescription,
                    SnapshotJson = v.SnapshotJson,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenario versions for {ScenarioId}", request.ScenarioId);
            return Result.Failure<List<ScenarioVersionDto>>("Error retrieving versions");
        }
    }
}
