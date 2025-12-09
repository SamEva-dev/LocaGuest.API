using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Commands.UpdatePreferences;

public record UpdatePreferencesCommand : IRequest<Result<UserPreferencesDto>>
{
    public bool DarkMode { get; init; }
    public string Language { get; init; } = "fr";
    public string Timezone { get; init; } = "Europe/Paris";
    public string DateFormat { get; init; } = "DD/MM/YYYY";
    public string Currency { get; init; } = "EUR";
    public bool SidebarNavigation { get; init; }
    public bool HeaderNavigation { get; init; }
}
