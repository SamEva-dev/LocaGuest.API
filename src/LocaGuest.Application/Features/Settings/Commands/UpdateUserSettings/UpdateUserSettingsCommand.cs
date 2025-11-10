using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Settings;
using MediatR;

namespace LocaGuest.Application.Features.Settings.Commands.UpdateUserSettings;

public record UpdateUserSettingsCommand : IRequest<Result<UserSettingsDto>>
{
    public UserProfileDto? Profile { get; init; }
    public NotificationSettingsDto? Notifications { get; init; }
    public PreferencesDto? Preferences { get; init; }
    public InterfaceSettingsDto? Interface { get; init; }
}
