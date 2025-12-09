using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Security;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Security.Queries.GetUserSessions;

public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<List<UserSessionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetUserSessionsQueryHandler> _logger;

    public GetUserSessionsQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<GetUserSessionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<UserSessionDto>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || !_tenantContext.UserId.HasValue)
                return Result.Failure<List<UserSessionDto>>("User not authenticated");

            var userId = _tenantContext.UserId.Value.ToString();
            var sessions = await _unitOfWork.UserSessions.GetActiveSessionsByUserIdAsync(userId, cancellationToken);

            var dtos = sessions.Select(s => new UserSessionDto
            {
                Id = s.Id,
                DeviceName = s.DeviceName,
                Browser = s.Browser,
                IpAddress = s.IpAddress,
                Location = s.Location,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsCurrent = false // TODO: Implement current session detection
            }).ToList();

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user sessions");
            return Result.Failure<List<UserSessionDto>>("Error retrieving sessions");
        }
    }
}
