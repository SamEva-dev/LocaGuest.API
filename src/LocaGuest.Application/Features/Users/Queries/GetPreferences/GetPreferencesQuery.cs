using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Queries.GetPreferences;

public record GetPreferencesQuery : IRequest<Result<UserPreferencesDto>>
{
}
