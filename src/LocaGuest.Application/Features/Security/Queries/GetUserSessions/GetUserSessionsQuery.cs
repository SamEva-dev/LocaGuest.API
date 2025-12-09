using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Security;
using MediatR;

namespace LocaGuest.Application.Features.Security.Queries.GetUserSessions;

public record GetUserSessionsQuery : IRequest<Result<List<UserSessionDto>>>
{
}
