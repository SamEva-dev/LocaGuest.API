using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Subscriptions.Queries.CheckQuota;

public record CheckQuotaQuery(string Dimension) : IRequest<Result<object>>;
