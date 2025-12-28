using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Addendums.Commands.DeleteAddendum;

public record DeleteAddendumCommand(Guid Id) : IRequest<Result<Guid>>;
