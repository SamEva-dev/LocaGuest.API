using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand : IRequest<Result<UserProfileDto>>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Company { get; init; }
    public string? Role { get; init; }
    public string? Bio { get; init; }
}
