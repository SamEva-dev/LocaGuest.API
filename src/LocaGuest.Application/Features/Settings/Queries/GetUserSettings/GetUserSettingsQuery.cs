using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Settings;
using MediatR;

namespace LocaGuest.Application.Features.Settings.Queries.GetUserSettings;

public record GetUserSettingsQuery : IRequest<Result<UserSettingsDto>>;
