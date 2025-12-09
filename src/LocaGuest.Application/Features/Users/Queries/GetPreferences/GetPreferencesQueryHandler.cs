using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Queries.GetPreferences;

public class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, Result<UserPreferencesDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetPreferencesQueryHandler> _logger;

    public GetPreferencesQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<GetPreferencesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<UserPreferencesDto>> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || !_tenantContext.UserId.HasValue)
                return Result.Failure<UserPreferencesDto>("User not authenticated");

            var userId = _tenantContext.UserId.Value.ToString();
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(userId, cancellationToken);

            // Si pas de préférences, créer des valeurs par défaut
            if (preferences == null)
            {
                preferences = UserPreferences.CreateDefault(userId);
                await _unitOfWork.UserPreferences.AddAsync(preferences, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            var dto = new UserPreferencesDto
            {
                DarkMode = preferences.DarkMode,
                Language = preferences.Language,
                Timezone = preferences.Timezone,
                DateFormat = preferences.DateFormat,
                Currency = preferences.Currency,
                SidebarNavigation = preferences.SidebarNavigation,
                HeaderNavigation = preferences.HeaderNavigation
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user preferences");
            return Result.Failure<UserPreferencesDto>("Error retrieving preferences");
        }
    }
}
