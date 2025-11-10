using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Settings;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Settings.Commands.UpdateUserSettings;

public class UpdateUserSettingsCommandHandler : IRequestHandler<UpdateUserSettingsCommand, Result<UserSettingsDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserSettingsCommandHandler> _logger;

    public UpdateUserSettingsCommandHandler(
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserSettingsCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserSettingsDto>> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId);
            
            // Get or create user settings
            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);

            if (userSettings == null)
            {
                userSettings = Domain.Aggregates.UserAggregate.UserSettings.Create(userId);
                _context.UserSettings.Add(userSettings);
            }

            // Update based on which section was provided
            if (request.Profile != null)
            {
                userSettings.UpdateProfile(request.Profile.PhotoUrl);
            }

            if (request.Notifications != null)
            {
                userSettings.UpdateNotifications(
                    request.Notifications.EmailAlerts,
                    request.Notifications.SmsAlerts,
                    request.Notifications.NewReservations,
                    request.Notifications.PaymentReminders,
                    request.Notifications.MonthlyReports
                );
            }

            if (request.Preferences != null)
            {
                userSettings.UpdatePreferences(
                    request.Preferences.DarkMode,
                    request.Preferences.Language,
                    request.Preferences.Timezone,
                    request.Preferences.DateFormat,
                    request.Preferences.Currency
                );
            }

            if (request.Interface != null)
            {
                userSettings.UpdateInterface(
                    request.Interface.SidebarNavigation,
                    request.Interface.HeaderNavigation
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User settings updated for user {UserId}", userId);
            
            // Return updated settings
            var settings = new UserSettingsDto
            {
                Profile = new UserProfileDto
                {
                    PhotoUrl = userSettings.PhotoUrl,
                    FirstName = "John", // TODO: Get from user entity
                    LastName = "Doe",
                    Email = "demo@locaguest.com",
                    Phone = "+33 6 12 34 56 78",
                    Company = "LocaGuest SARL",
                    Role = "Propriétaire",
                    Bio = "Gestionnaire immobilier"
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
            _logger.LogError(ex, "Error updating user settings");
            return Result.Failure<UserSettingsDto>("Error updating user settings");
        }
    }
}
