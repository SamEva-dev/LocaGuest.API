using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Commands.UpdatePreferences;

public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, Result<UserPreferencesDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdatePreferencesCommandHandler> _logger;

    public UpdatePreferencesCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdatePreferencesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<UserPreferencesDto>> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || !_tenantContext.UserId.HasValue)
                return Result.Failure<UserPreferencesDto>("User not authenticated");

            var userId = _tenantContext.UserId.Value.ToString();
            var preferences = await _unitOfWork.UserPreferences.GetByUserIdAsync(userId, cancellationToken);
            
            if (preferences == null)
            {
                preferences = UserPreferences.CreateDefault(userId);
                await _unitOfWork.UserPreferences.AddAsync(preferences, cancellationToken);
            }

            preferences.Update(
                request.DarkMode,
                request.Language,
                request.Timezone,
                request.DateFormat,
                request.Currency,
                request.SidebarNavigation,
                request.HeaderNavigation);

            await _unitOfWork.CommitAsync(cancellationToken);

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
            _logger.LogError(ex, "Error updating user preferences");
            return Result.Failure<UserPreferencesDto>("Error updating preferences");
        }
    }
}
