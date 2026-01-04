using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Admin.Queries.GetDatabaseStats;

public record GetDatabaseStatsQuery : IRequest<Result<object>>;
