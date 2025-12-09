using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Queries.GetNotifications;

public record GetNotificationsQuery : IRequest<Result<NotificationSettingsDto>>
{
}
