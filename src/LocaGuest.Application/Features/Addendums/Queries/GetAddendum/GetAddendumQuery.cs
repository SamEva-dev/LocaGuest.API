using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Addendums;
using MediatR;

namespace LocaGuest.Application.Features.Addendums.Queries.GetAddendum;

public record GetAddendumQuery(Guid Id) : IRequest<Result<AddendumDto>>;
