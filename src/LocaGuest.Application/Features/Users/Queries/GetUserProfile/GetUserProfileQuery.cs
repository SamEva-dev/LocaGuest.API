using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Queries.GetUserProfile;

public record GetUserProfileQuery : IRequest<Result<UserProfileDto>>
{
}
