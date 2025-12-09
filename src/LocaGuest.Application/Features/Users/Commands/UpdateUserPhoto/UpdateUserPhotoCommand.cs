using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Commands.UpdateUserPhoto;

public record UpdateUserPhotoCommand : IRequest<Result<UserProfileDto>>
{
    public string PhotoUrl { get; init; } = string.Empty;
}
