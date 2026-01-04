using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Admin.Commands.CleanDatabase;

public record CleanDatabaseCommand : IRequest<Result<CleanDatabaseDto>>
{
    public string PreservedUserId { get; init; } = string.Empty;
}

public record CleanDatabaseDto(string PreservedUser, string[] DeletedTables);
