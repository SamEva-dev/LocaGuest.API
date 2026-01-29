using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizationSessions;

public sealed class GetProvisioningOrganizationSessionsQueryHandler
    : IRequestHandler<GetProvisioningOrganizationSessionsQuery, Result<List<ProvisioningOrganizationSessionDto>>>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<GetProvisioningOrganizationSessionsQueryHandler> _logger;

    public GetProvisioningOrganizationSessionsQueryHandler(
        ILocaGuestDbContext db,
        ILogger<GetProvisioningOrganizationSessionsQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<ProvisioningOrganizationSessionDto>>> Handle(
        GetProvisioningOrganizationSessionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var orgId = request.OrganizationId;

            var userIds = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.OrganizationId == orgId && tm.IsActive)
                .Select(tm => tm.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (userIds.Count == 0)
                return Result.Success(new List<ProvisioningOrganizationSessionDto>());

            // NOTE: UserSession.UserId is stored as string in the domain.
            var userIdStrings = userIds.Select(x => x.ToString()).ToList();

            var sessions = await _db.UserSessions
                .AsNoTracking()
                .Where(s => userIdStrings.Contains(s.UserId) && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .Take(2000)
                .Select(s => new ProvisioningOrganizationSessionDto
                {
                    Id = s.Id,
                    UserId = Guid.Parse(s.UserId),
                    DeviceName = s.DeviceName,
                    Browser = s.Browser,
                    IpAddress = s.IpAddress,
                    Location = s.Location,
                    CreatedAt = s.CreatedAt,
                    LastActivityAt = s.LastActivityAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provisioning sessions for org {OrganizationId}", request.OrganizationId);
            return Result.Failure<List<ProvisioningOrganizationSessionDto>>($"Failed to get sessions: {ex.Message}");
        }
    }
}
