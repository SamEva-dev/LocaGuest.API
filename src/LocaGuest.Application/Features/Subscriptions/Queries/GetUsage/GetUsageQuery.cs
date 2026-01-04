using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetUsage;

public record GetUsageQuery : IRequest<Result<object>>;
