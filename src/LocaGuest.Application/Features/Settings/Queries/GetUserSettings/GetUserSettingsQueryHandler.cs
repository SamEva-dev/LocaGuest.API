using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Settings;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Settings.Queries.GetUserSettings;

public class GetUserSettingsQueryHandler : IRequestHandler<GetUserSettingsQuery, Result<UserSettingsDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserSettingsQueryHandler> _logger;

    public GetUserSettingsQueryHandler(
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetUserSettingsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserSettingsDto>> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            
            // Get or create user settings
            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);

            if (userSettings == null)
            {
                // Create default settings for this user
                userSettings = Domain.Aggregates.UserAggregate.UserSettings.Create(userId);
                _context.UserSettings.Add(userSettings);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Map to DTO
            var settings = new UserSettingsDto
            {
                Profile = new UserProfileDto
                {
                    FirstName = "John", // TODO: Get from user entity
                    LastName = "Doe",   // TODO: Get from user entity
                    Email = "demo@locaguest.com", // TODO: Get from user entity
                    Phone = "+33 6 12 34 56 78",  // TODO: Get from user entity
                    Company = "LocaGuest SARL",   // TODO: Get from user entity
                    Role = "Propriétaire",         // TODO: Get from user entity
                    Bio = "Gestionnaire immobilier avec 5 ans d'expérience.", // TODO: Get from user entity
                    PhotoUrl = userSettings.PhotoUrl
                },
                Notifications = new NotificationSettingsDto
                {
                    EmailAlerts = userSettings.EmailAlerts,
                    SmsAlerts = userSettings.SmsAlerts,
                    NewReservations = userSettings.NewReservations,
                    PaymentReminders = userSettings.PaymentReminders,
                    MonthlyReports = userSettings.MonthlyReports
                },
                Preferences = new PreferencesDto
                {
                    DarkMode = userSettings.DarkMode,
                    Language = userSettings.Language,
                    Timezone = userSettings.Timezone,
                    DateFormat = userSettings.DateFormat,
                    Currency = userSettings.Currency
                },
                Interface = new InterfaceSettingsDto
                {
                    SidebarNavigation = userSettings.SidebarNavigation,
                    HeaderNavigation = userSettings.HeaderNavigation
                }
            };

            return Result.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user settings");
            return Result.Failure<UserSettingsDto>("Error retrieving user settings");
        }
    }
}
